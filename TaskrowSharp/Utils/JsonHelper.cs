﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TaskrowSharp.Utils
{
    internal class JsonHelper
    {
        public static string Serialize(object obj)
        {
            return SimpleJson.SimpleJson.SerializeObject(obj, new SimpleJson.DataContractJsonSerializerStrategy());
        }
        
        public static T Deserialize<T>(string json)
        {
            try
            {
                return (T)SimpleJson.SimpleJson.DeserializeObject(json, typeof(T), new SimpleJson.DataContractJsonSerializerStrategy());
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                System.Diagnostics.Debugger.Break();
                throw;
            }
        }
    }
}
