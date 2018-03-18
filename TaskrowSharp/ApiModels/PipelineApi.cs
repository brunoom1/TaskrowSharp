﻿using System;
using System.Collections.Generic;
using System.Text;

namespace TaskrowSharp.ApiModels
{
    internal class PipelineApi
    {
        public int PipelineID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool CompanyDefault { get; set; }
        public bool Extranet { get; set; }
        public bool ResetOnRequestTypeChange { get; set; }
        public List<PipelineStepApi> PipelineSteps { get; set; }
    }
}
