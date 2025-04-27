using System;
using System.Collections.Generic;
using System.Linq;
using Melanchall.DryWetMidi.Multimedia;
using UnityEngine;

namespace OutputDeviceHandling
{
    public class DeviceConnector: MonoBehaviour
    {
        [SerializeField] private GameObject defaultComputerOutputPlayer;
    
        private IInputDevice _inputDevice;
        private IOutputDevice _outputDevice;
    
        private DevicesConnector _devicesConnector;
    
        /// <summary>
        /// Caches output devices to prevent reusing disposed devices.
        /// </summary>
        private readonly Dictionary<string, OutputDevice> _outputDeviceCache = new Dictionary<string, OutputDevice>();
    
        /// <summary>
        /// Retrieves a list of available input devices, excluding ignored devices.
        /// </summary>
        /// <returns>List of input device names</returns>
        public List<string> GetInputDevices()
        {
            try
            {
                var inputDevices = InputDevice.GetAll()
                    .Select(x => x.Name)
                    .ToList();
                
                inputDevices.Add(Constants.DefaultInputDevice);
                return inputDevices
                    .Except(Constants.IgnoredDevices ?? Enumerable.Empty<string>())
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error retrieving input devices: {ex.Message}");
                return new List<string> { Constants.DefaultInputDevice };
            }
        }
    
        /// <summary>
        /// Retrieves a list of available output devices, excluding ignored devices.
        /// </summary>
        /// <returns>List of output device names</returns>
        public List<string> GetOutputDevices()
        {
            try
            {
                var outputDevices = OutputDevice.GetAll()
                    .Select(x => x.Name)
                    .ToList();
                
                outputDevices.Add(Constants.DefaultOutputDevice);
                return outputDevices
                    .Except(Constants.IgnoredDevices ?? Enumerable.Empty<string>())
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error retrieving output devices: {ex.Message}");
                return new List<string> { Constants.DefaultOutputDevice };
            }
        }
        
        /// <summary>
        /// Disconnects the current device connector.
        /// </summary>
        private void DisconnectConnector()
        {
            _devicesConnector?.Disconnect();
            Debug.Log("Midi connector disconnected");
        }

        /// <summary>
        /// Sets the input device for connection.
        /// </summary>
        /// <param name="inputDevice">Input device to set</param>
        public void SetInputDevice(IInputDevice inputDevice)
        {
            _inputDevice = inputDevice ?? throw new ArgumentNullException(nameof(inputDevice), "Input device cannot be null");
        }

        /// <summary>
        /// Sets the output device, using a cached device or creating a new one.
        /// </summary>
        /// <param name="outputDevice">Name of the output device</param>
        public void SetOutputDevice(string outputDevice)
        {
            if (string.IsNullOrWhiteSpace(outputDevice))
            {
                throw new ArgumentException("Output device name cannot be null or empty", nameof(outputDevice));
            }

            try
            {
                if (outputDevice == Constants.DefaultOutputDevice)
                {
                    _outputDevice = defaultComputerOutputPlayer.GetComponent<IOutputDevice>();
                }
                else
                {
                    // Use cached device or create new one
                    if (!_outputDeviceCache.TryGetValue(outputDevice, out var cachedDevice))
                    {
                        cachedDevice = OutputDevice.GetByName(outputDevice) 
                                       ?? throw new InvalidOperationException($"Could not find output device: {outputDevice}");
                        
                        _outputDeviceCache[outputDevice] = cachedDevice;
                    }
                    _outputDevice = cachedDevice;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error setting output device: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Disconnects the devices if they are currently connected.
        /// </summary>
        public void Disconnect()
        {
            if (_devicesConnector == null || !_devicesConnector.AreDevicesConnected)
            {
                return;
            }
            _devicesConnector.Disconnect();
        }
    
        /// <summary>
        /// Connects the devices if they are not already connected.
        /// </summary>
        public void Connect()
        {
            if (_devicesConnector == null || _devicesConnector.AreDevicesConnected)
            {
                return;
            }
            _devicesConnector.Connect();
        }
    
        /// <summary>
        /// Creates a new device connection, disconnecting any existing connection.
        /// </summary>
        public void CreateConnection()
        {
            try
            {
                if (_inputDevice == null)
                {
                    throw new InvalidOperationException("Input device is not set");
                }

                if (_outputDevice == null)
                {
                    throw new InvalidOperationException("Output device is not set");
                }

                DisconnectConnector();

                _devicesConnector = new DevicesConnector(_inputDevice, _outputDevice);
                _devicesConnector.Connect();
                
                Debug.Log("Midi connector connected");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating MIDI connection: {ex.Message}");
                throw; // Re-throw to allow caller to handle
            }
        }

        
        /// <summary>
        /// Cleanup method to disconnect devices and dispose of cached output devices.
        /// </summary>
        public void OnDestroy()
        {
            try
            {
                DisconnectConnector();

                // Dispose of cached output devices
                foreach (var outputDevice in _outputDeviceCache.Values)
                {
                    try
                    {
                        outputDevice?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error disposing output device: {ex.Message}");
                    }
                }

                _outputDeviceCache.Clear();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during device cleanup: {ex.Message}");
            }
        }
    }
}
