using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace PythonCommunication
{
    /// <summary>
    /// Server response data class
    /// </summary>
    [Serializable]
    public class ServerResponse
    {
        public int statusCode;
        public string content;
    }

    /// <summary>
    /// Server request dataclass
    /// </summary>
    [Serializable]
    public class ServerRequest
    {
        public string requestType;
        public Dictionary<string, dynamic> parameters;
    }
}