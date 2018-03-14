﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace TaskrowSharp
{
    public class TaskrowClient
    {
        private Uri ServiceUrl = null;
        private CookieCollection Cookies;

        public string UserAgent { get; set; }

        public bool StatusConnected
        {
            get { return (this.ServiceUrl != null); }
        }

        public TaskrowClient()
        {
            this.UserAgent = string.Format("TaskrowSharp v{0}", Utils.Application.GetAppVersion());
        }
        
        #region Connect

        public void Connect(Uri serviceUrl, string email, string password, int maxAttempts = 1, int timeOutSeconds = 120)
        {
            Connect(serviceUrl, new EmailAndPasswordCredential(email, password), maxAttempts, timeOutSeconds);
        }

        public void Connect(Uri serviceUrl, Credential credential, int maxAttempts = 1, int timeOutSeconds = 120)
        {
            ValidateServiceUrl(serviceUrl);
            credential.Validate();            

            if (credential == null)
                throw new ArgumentNullException(nameof(credential));
            
            credential.Validate();

            if (credential is EmailAndPasswordCredential)
                ConnectUsingEmailAndPassword(serviceUrl, (EmailAndPasswordCredential)credential, maxAttempts, timeOutSeconds);
            else
                throw new TaskrowException("Credential Type not supported");
        }

        private void ConnectUsingEmailAndPassword(Uri serviceUrl, EmailAndPasswordCredential credential, int maxAttempts = 1, int timeOutSeconds = 120)
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    this.ServiceUrl = null;

                    Uri url = new Uri(serviceUrl, "/LoginPassword");
                    string myParameters = string.Format("email={0}&password={1}", System.Web.HttpUtility.UrlEncode(credential.Email), System.Web.HttpUtility.UrlEncode(credential.Password));

                    System.Net.HttpWebRequest request = HttpWebRequest.Create(url) as HttpWebRequest;
                    request.Method = "POST";
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.UserAgent = UserAgent;
                    request.AllowAutoRedirect = false;
                    request.CookieContainer = new CookieContainer();
                    request.Timeout = timeOutSeconds * 1000;
                    using (var writer = new System.IO.StreamWriter(request.GetRequestStream()))
                    {
                        writer.Write(myParameters);
                        writer.Flush();
                    }

                    var response = request.GetResponse() as HttpWebResponse;
                    Utils.JsonWebClient.CheckHttpStatusCode(response, url, true);

                    string cookie1 = null;
                    string cookie2 = null;
                    var cookies = response.Cookies;
                    if (response.Cookies["AUTHTICKET"] != null)
                        cookie1 = response.Cookies["AUTHTICKET"].Value;
                    if (response.Cookies["AUTHTICKETVOL"] != null)
                        cookie2 = response.Cookies["AUTHTICKETVOL"].Value;

                    if (string.IsNullOrEmpty(cookie1) || string.IsNullOrEmpty(cookie2))
                        throw new AuthenticationException(string.Format("Error connecting in Taskrow. Check parameters: ServiceUrl, Email and Password -- URL: {0} -- email: {1}", url.ToString(), credential.Email));

                    this.ServiceUrl = serviceUrl;
                    this.Cookies = cookies;
                    response.Close();

                    return; //Success
                }
                catch (System.Exception ex)
                {
                    if (attempt == maxAttempts)
                        throw new AuthenticationException(string.Format("Error connecting in Taskrow after {0} attempts(s) -- url: {1} -- email: {2} -- {3} -- TimeOut: {4} seconds", maxAttempts, serviceUrl.ToString(), credential.Email, ex.Message, timeOutSeconds), ex);
                }
            }

            throw new TaskrowException("Unexpected error in attempts control");
        }

        private void ValidateServiceUrl(Uri serviceUrl)
        {
            if (serviceUrl == null)
                throw new InvalidServiceUrlException("Service url is required");

            if (serviceUrl.ToString().StartsWith("http://", StringComparison.CurrentCultureIgnoreCase))
                throw new InvalidServiceUrlException("https:// is required");
                        
            if (!serviceUrl.ToString().StartsWith("https://", StringComparison.CurrentCultureIgnoreCase) || !serviceUrl.ToString().EndsWith(".taskrow.com/", StringComparison.CurrentCultureIgnoreCase))
                throw new InvalidServiceUrlException("Invalid service url, use the format: https://yourcompany.taskrow.com");
        }

        private void ValidateIsConnected()
        {
            if (!this.StatusConnected)
                throw new NotConnectedException();
        }

        public void Disconnect()
        {
            if (!StatusConnected)
                return;

            this.ServiceUrl = null;
            this.Cookies = null;
        }

        public string KeepAlive()
        {
            //Example: /Administrative/ListGroups?groupTypeID=2

            ValidateIsConnected();

            var url = new Uri(this.ServiceUrl, "/main/keepalive");

            try
            {
                var client = new Utils.JsonWebClient();
                client.TimeOutMilliseconds = 5000; //5 seconds
                client.Cookies.Add(this.Cookies);

                return client.GetReturnString(url);
            }
            catch (System.Exception ex)
            {
                throw new TaskrowException(string.Format("Error in KeepAlive process -- Url: {0} -- Error: {1}", url.ToString(), ex.Message), ex);
            }
        }

        #endregion
        
        #region User

        public User GetUserByEmail(string email, int maxAttempts = 1, int timeOutSeconds = 60)
        {
            var list = this.ListUsers(maxAttempts, timeOutSeconds);
            var user = list.FirstOrDefault(a => a.MainEmail.Equals(email, StringComparison.CurrentCultureIgnoreCase));
            return user;
        }

        public List<User> ListUsers(int maxAttempts = 1, int timeOutSeconds = 60)
        {
            //Example: /User/TeamHome

            ValidateIsConnected();

            var url = new Uri(this.ServiceUrl, "/User/TeamHome");
            
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    var listUsers = new List<User>();

                    var client = new Utils.JsonWebClient();
                    client.TimeOutMilliseconds = timeOutSeconds * 1000;
                    client.Cookies.Add(this.Cookies);

                    string json = client.GetReturnString(url);

                    var userListResponse = Utils.JsonHelper.Deserialize<ApiModels.UserListApiResponse>(json);

                    foreach (var userResponse in userListResponse.Users)
                        listUsers.Add(new User(userResponse));
                                        
                    return listUsers; //Success
                }
                catch (System.Exception ex)
                {
                    if (attempt == maxAttempts)
                        throw new TaskrowException(string.Format("Error listing users after {0} attempt(s) -- Url: {1} -- Error: {2} -- TimeOut: {3} seconds", maxAttempts, url.ToString(), ex.Message, timeOutSeconds), ex);
                }
            }

            throw new TaskrowException("Unexpected error in attempts control");
        }

        #endregion

        #region Group (not implemented)

        //public Group GetGroupByName(string name, int maxAttempts = 1, int timeOutSeconds = 60)
        //{
        //    var list = this.ListGroups(maxAttempts, timeOutSeconds);
        //    var group = list.FirstOrDefault(a => a.GroupName.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        //    return group;
        //}

        //public List<Group> ListGroups(int maxAttempts = 1, int timeOutSeconds = 60)
        //{
        //    //Example: /Administrative/ListGroups?groupTypeID=2

        //    ValidateStatusConnected();

        //    var url = new Uri(this.ServiceUrl, "/Administrative/ListGroups?groupTypeID=2");

        //    for (int attempt = 1; attempt <= maxAttempts; attempt++)
        //    {
        //        try
        //        {
        //            var client = new Utils.JsonWebClient();
        //            client.TimeOutMilliseconds = timeOutSeconds * 1000;
        //            client.Cookies.Add(this.Cookies);

        //            var jObject = client.GetReturnJObject(url);

        //            var listGroups = new List<Group>();
        //            foreach (var item in jObject["Groups"])
        //            {
        //                var group = new Group();
        //                group.GroupID = Utils.Parser.ToInt32(item["GroupID"]);
        //                group.GroupName = item["GroupName"].ToString();
        //                listGroups.Add(group);
        //            }

        //            return listGroups; //Success
        //        }
        //        catch (System.Exception ex)
        //        {
        //            if (attempt == maxAttempts)
        //                throw new TaskrowException(string.Format("Error listing groups after {0} attempts(s) -- url: {1} -- Error: {2} -- TimeOut: {3} seconds", maxAttempts, url.ToString(), ex.Message, timeOutSeconds), ex);
        //        }
        //    }

        //    throw new TaskrowException("Unexpected error in attempts control");
        //}

        #endregion

        #region Task (not implemented)

        //public List<Task> ListTasksOpenByGroup(int groupID, int? userID = null, int maxAttempts = 1, int timeOutSeconds = 60)
        //{
        //    //Example: /Dashboard/TasksByGroup?groupID=421&hierarchyEnabled=true&userID=3564&closedDays=20&context=1

        //    ValidateStatusConnected();

        //    var url = new Uri(this.ServiceUrl, string.Format("/Dashboard/TasksByGroup?groupID={0}&hierarchyEnabled=true&closedDays=20&context=1", groupID)).ToString();
        //    if (userID.HasValue)
        //        url = string.Format("{0}&userID={1}", url, userID);

        //    for (int attempt = 1; attempt <= maxAttempts; attempt++)
        //    {
        //        try
        //        {
        //            var client = new Utils.JsonWebClient();
        //            client.TimeOutMilliseconds = timeOutSeconds * 1000;
        //            client.Cookies.Add(this.Cookies);

        //            var jObject = client.GetReturnJObject(new Uri(url));

        //            var listTasks = new List<Task>();
        //            foreach (var item in jObject["Entity"]["OpenTasks"])
        //            {
        //                var task = GeTaskFromJToken(item);
        //                listTasks.Add(task);
        //            }

        //            return listTasks; //Success
        //        }
        //        catch (System.Exception ex)
        //        {
        //            if (attempt == maxAttempts)
        //                throw new TaskrowException(string.Format("Erro listing open tasks from group after {0} attempts(s) -- url: {1} -- Error: {2} -- TimeOut: {3} seconds", maxAttempts, url.ToString(), ex.Message, timeOutSeconds), ex);
        //        }
        //    }

        //    throw new TaskrowException("Unexpected error in attempts control");
        //}

        //private Task GeTaskFromJToken(Newtonsoft.Json.Linq.JToken item)
        //{
        //    var task = new Task();
        //    task.TaskNumber = Utils.Parser.ToInt32(item["TaskNumber"]);
        //    task.TaskID = Utils.Parser.ToInt32(item["TaskID"]);
        //    task.TaskTitle = item["TaskTitle"].ToString();
        //    task.CreationDate = Convert.ToDateTime(item["CreationDate"]);
        //    task.DueDate = Convert.ToDateTime(item["DueDate"]);
        //    task.JobID = Utils.Parser.ToInt32(item["JobID"]);
        //    task.JobNumber = Utils.Parser.ToInt32(item["JobNumber"]);
        //    task.ClientNickName = item["ClientNickName"].ToString();
        //    task.OwnerUserID = Convert.ToInt32(item["OwnerUserID"]);
        //    return task;
        //}

        //public TaskDetail GetTaskDetail(int jobNumber, int taskNumber, string clientNickName, int maxAttempts = 1, int timeOutSeconds = 120)
        //{
        //    //Example: /Task/TaskDetail?clientNickName=[clientName]&jobNumber=[job number]&taskNumber=[task number]

        //    ValidateStatusConnected();

        //    var url = new Uri(this.ServiceUrl, string.Format("/Task/TaskDetail?jobNumber={0}&taskNumber={1}&clientNickName={2}", jobNumber, taskNumber, clientNickName));

        //    for (int attempt = 1; attempt <= maxAttempts; attempt++)
        //    {
        //        try
        //        {
        //            var client = new Utils.JsonWebClient();
        //            client.TimeOutMilliseconds = timeOutSeconds * 1000;
        //            client.Cookies.Add(this.Cookies);

        //            var jObject = client.GetReturnJObject(url);

        //            var taskData = jObject["TaskData"];
        //            var jobData = jObject["JobData"];

        //            var taskDetail = GetTaskDetailFromJson(taskData, jobData);

        //            return taskDetail; //Success
        //        }
        //        catch (System.Exception ex)
        //        {
        //            if (attempt == maxAttempts)
        //                throw new TaskrowException(string.Format("Error getting Task details after {0} attempts(s) -- Url: {1} -- Error: {2} -- TimeOut: {3} seconds", maxAttempts, url.ToString(), ex.Message, timeOutSeconds), ex);
        //        }
        //    }

        //    throw new TaskrowException("Unexpected error in attempts control");
        //}

        //private TaskDetail GetTaskDetailFromJson(Newtonsoft.Json.Linq.JToken taskData, Newtonsoft.Json.Linq.JToken jobData)
        //{
        //    var taskDetail = new TaskDetail();

        //    taskDetail.TaskID = Utils.Parser.ToInt32(taskData["TaskID"]);
        //    taskDetail.TaskNumber = Utils.Parser.ToInt32(taskData["TaskNumber"]);
        //    taskDetail.TaskTitle = taskData["TaskTitle"].ToString();
        //    taskDetail.MemberListString = taskData["MemberListString"].ToString();
        //    taskDetail.RowVersion = taskData["RowVersion"].ToString();
        //    taskDetail.DueDate = Convert.ToDateTime(taskData["DueDate"].ToString());

        //    taskDetail.TaskItems = new List<TaskItem>();
        //    foreach (var item in taskData["NewTaskItems"])
        //    {
        //        var taskItem = new TaskItem();
        //        taskItem.TaskItemID = Utils.Parser.ToInt32(item["TaskItemID"]);
        //        taskItem.OldOwnerUserID = Utils.Parser.ToInt32(item["OldOwnerUserID"]);
        //        taskItem.OldOwnerName = item["OldOwnerName"].ToString();
        //        taskItem.NewOwnerUserID = Utils.Parser.ToInt32(item["NewOwnerUserID"]);
        //        taskItem.NewOwnerName = item["NewOwnerName"].ToString();
        //        taskItem.TaskItemComment = item["TaskItemComment"].ToString();
        //        taskDetail.TaskItems.Add(taskItem);
        //    }

        //    taskDetail.JobID = Utils.Parser.ToInt32(jobData["JobID"]);
        //    taskDetail.JobNumber = Utils.Parser.ToInt32(jobData["JobNumber"]);
        //    taskDetail.JobTitle = jobData["JobTitle"].ToString();

        //    var clientData = jobData["Client"];
        //    taskDetail.ClientNickName = clientData["ClientNickName"].ToString();

        //    taskDetail.Tags = new List<TaskTag>();
        //    foreach (var item in taskData["Tags"])
        //        taskDetail.Tags.Add(new TaskTag() { TaskTagID = Utils.Parser.ToInt32(item["TaskTagID"]), TagTitle = item["TagTitle"].ToString() });

        //    taskDetail.SubTasks = new List<SubTask>();
        //    if (taskData["Subtasks"] != null)
        //    {
        //        foreach (var item in taskData["Subtasks"])
        //        {
        //            var subTask = new SubTask();
        //            subTask.SubtaskID = Utils.Parser.ToInt32(item["SubtaskID"]);
        //            subTask.TaskID = Utils.Parser.ToInt32(item["TaskID"]);
        //            subTask.ChildTaskID = Utils.Parser.ToInt32(item["ChildTaskID"]);
        //            subTask.Title = item["Title"].ToString();

        //            var childTaskData = item["ChildTask"];
        //            if (childTaskData != null)
        //            {
        //                TaskDetail childTask = GetTaskDetailFromJson(childTaskData, jobData);
        //                subTask.ChildTask = childTask;
        //            }

        //            taskDetail.SubTasks.Add(subTask);
        //        }
        //    }

        //    return taskDetail;
        //}

        #endregion

        #region SaveTask (not implemented)

        //public void SaveTask(SaveTaskRequest request, int maxAttempts = 1, int timeOutSeconds = 120)
        //{
        //    //Example: /Task/SaveTask

        //    ValidateStatusConnected();

        //    var url = new Uri(this.ServiceUrl, "/Task/SaveTask");

        //    for (int attempt = 1; attempt <= maxAttempts; attempt++)
        //    {
        //        try
        //        {
        //            var client = new Utils.JsonWebClient();
        //            client.TimeOutMilliseconds = timeOutSeconds * 1000;
        //            client.Cookies.Add(this.Cookies);
        //            client.AllowAutoRedirect = false;

        //            //client.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
        //            client.Host = this.ServiceUrl.Host;
        //            client.Origin = this.ServiceUrl.ToString();
        //            client.Referer = this.ServiceUrl.ToString();
        //            client.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/36.0.1985.143 Safari/537.36";

        //            var values = new System.Collections.Specialized.NameValueCollection();
        //            values.Add("jobNumber", request.JobNumber.ToString());
        //            values.Add("clientNickName", request.ClientNickName);
        //            values.Add("lastTaskItemID", request.LastTaskItemID.ToString());
        //            values.Add("TaskID", request.TaskID.ToString());
        //            values.Add("MemberListString", request.MemberListString);
        //            values.Add("TaskNumber", request.TaskNumber.ToString());

        //            if (!string.IsNullOrEmpty(request.RowVersion))
        //                values.Add("RowVersion", request.RowVersion);

        //            values.Add("TaskTitle", request.TaskTitle);
        //            values.Add("TaskItemComment", request.TaskItemComment);
        //            values.Add("OwnerUserID", request.OwnerUserID.ToString());
        //            values.Add("SpentTime", request.SpentTime.ToString());
        //            values.Add("DueDate", request.DueDate.ToString("yyyy-MM-dd"));
        //            values.Add("PercentComplete", request.PercentComplete.ToString());

        //            var jObject = client.PostValuesReturnJObject(url, values);

        //            if (System.Diagnostics.Debugger.IsAttached)
        //            {
        //                StringBuilder sb = new StringBuilder();
        //                foreach (var key in values.AllKeys)
        //                    sb.AppendFormat("{0}={1}\r\n", key, values[key]);

        //                string jsonRequest = sb.ToString();

        //                string jsonReponse = jObject.ToString();

        //                System.Diagnostics.Debug.WriteLine(string.Format("Taskrow -- url= {0}", url));
        //                System.Diagnostics.Debug.WriteLine(string.Format("Taskrow -- request= {0}", jsonRequest));
        //                System.Diagnostics.Debug.WriteLine(string.Format("Taskrow -- response= {0}", jsonReponse));
        //            }

        //            bool success = Convert.ToBoolean(jObject["Success"]);
        //            string message = jObject["Message"].ToString();

        //            if (!success)
        //                throw new TaskrowException(message);

        //            return; //Success
        //        }
        //        catch (System.Exception ex)
        //        {
        //            if (attempt == maxAttempts)
        //                throw new TaskrowException(string.Format("Error saving task after {0} attempts(s) -- url: {1} -- Error: {2} -- TimeOut: {3} seconds", maxAttempts, url.ToString(), ex.Message, timeOutSeconds), ex);
        //        }
        //    }

        //    throw new TaskrowException("Unexpected error in attempts control");
        //}

        #endregion
    }
}