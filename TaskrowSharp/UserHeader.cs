﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TaskrowSharp
{
    public class UserHeader
    {
        public int UserID { get; set; }

        public string FullName { get; set; }

        public string MainEmail { get; set; }

        public string UserLogin { get; set; }

        public bool Active { get; set; }

        public int AppMainCompanyID { get; set; }

        public string UserHashCode { get; set; }

        public string PhotoUrl { get { return string.Format("{0}.jpg", this.UserHashCode); } }

        public string ApprovalGroup { get; set; }

        public string ProfileTitle { get; set; }

        public UserHeader(int userID, string fullName, string mainEmail, string userLogin, bool active, int appMainCompanyID, string userHashCode, 
            string photoUrl, string approvalGroup, string profileTitle)
        {
            this.UserID = userID;
            this.FullName = fullName;
            this.MainEmail = mainEmail;
            this.UserLogin = userLogin;
            this.Active = active;
            this.AppMainCompanyID = appMainCompanyID;
            this.UserHashCode = userHashCode;
            this.ApprovalGroup = approvalGroup;
            this.ProfileTitle = profileTitle;
        }

        internal UserHeader(ApiModels.UserHeaderApi userApi)
        {
            this.UserID = userApi.UserID;
            this.FullName = userApi.FullName;
            this.MainEmail = userApi.MainEmail;
            this.UserLogin = userApi.UserLogin;
            this.Active = !userApi.Inactive;
            this.AppMainCompanyID = userApi.AppMainCompanyID;
            this.UserHashCode = userApi.UserHashCode;
            this.ApprovalGroup = userApi.ApprovalGroup;
            this.ProfileTitle = userApi.ProfileTitle;
        }
    }
}
