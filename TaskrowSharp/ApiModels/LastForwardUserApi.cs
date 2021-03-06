﻿using System;
using System.Collections.Generic;
using System.Text;

namespace TaskrowSharp.ApiModels
{
    internal class LastForwardUserApi
    {
        public int UserID { get; set; }
        public string UserLogin { get; set; }
        public string UserHashCode { get; set; }
        public string FullName { get; set; }
        public object ExternalCode { get; set; }
        public int PhotoVersion { get; set; }
        public bool Creator { get; set; }
    }
}
