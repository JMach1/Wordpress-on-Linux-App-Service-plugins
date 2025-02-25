﻿using Newtonsoft.Json;
using Renci.SshNet.Sftp;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace WordPressMigrationTool.Utilities
{
    public static class HelperUtils
    {

        public static string GetKuduApiForZipDownload(string appServiceName)
        {
            if (!string.IsNullOrWhiteSpace(appServiceName))
            {
                return "https://" + appServiceName + ".scm.azurewebsites.net/api/zip/site/wwwroot/wp-content/";
            }
            return null;
        }

        public static string GetKuduApiForZipUpload(string appServiceName, string uploadPath)
        {
            if (!string.IsNullOrWhiteSpace(appServiceName))
            {
                return "https://" + appServiceName + ".scm.azurewebsites.net/api/zip/" + uploadPath;
            }
            return null;
        }

        // Returns Kudu API URL for the given appservice
        public static string GetKuduApiForCommandExec(string appServiceName)
        {
            if (!string.IsNullOrWhiteSpace(appServiceName))
            {
                return "https://" + appServiceName + ".scm.azurewebsites.net/api/command";
            }
            return null;
        }

        public static string GetMySQLConnectionStringForExternalMySQLClientTool(string serverHostName,
            string username, string password, string databaseName, string? charset)
        {
            if (string.IsNullOrWhiteSpace(serverHostName) || string.IsNullOrWhiteSpace(username)
                || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(databaseName))
            {
                return null;
            }

            string mysqlConnectionString = "server=" + serverHostName + ";user=" + username + ";pwd='"
                + password + "';database=" + databaseName + ";convertzerodatetime=true;";
            if (!string.IsNullOrWhiteSpace(charset))
            {
                return mysqlConnectionString + "charset=" + charset + ";";
            }

            return mysqlConnectionString;
        }

        // Parses Database connection string of windows app service to get DB info (hostname, username, password and database name)
        public static Result ParseAndUpdateDatabaseConnectionStringForWinAppService(SiteInfo sourceSite, string databaseConnectionString)
        {
            sourceSite.databaseName = GetValueFromDbConnectionString("Database", databaseConnectionString);
            sourceSite.databaseHostname = GetValueFromDbConnectionString("Data Source", databaseConnectionString);
            sourceSite.databaseUsername = GetValueFromDbConnectionString("User Id", databaseConnectionString);
            sourceSite.databasePassword = GetValueFromDbConnectionString("Password", databaseConnectionString);

            if (String.IsNullOrEmpty(sourceSite.databaseName) || String.IsNullOrEmpty(sourceSite.databaseHostname) || String.IsNullOrEmpty(sourceSite.databaseUsername) || String.IsNullOrEmpty(sourceSite.databasePassword))
            {
                return new Result(Status.Failed, "Couldn't retrieve database credentials from the Database connection string");
            }
            return new Result(Status.Completed, "");
        }

        private static string GetValueFromDbConnectionString(string key, string inputString)
        {
            // DB connection string regular expression
            var pattern = @"(.+)=(.+);(.+)=(.+);(.+)=(.+);(.+)=(.+)";
            var match = Regex.Match(inputString, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                if (match.Groups[1].Value.Contains(key))
                {
                    return match.Groups[2].Value;
                }
                else if (match.Groups[3].Value.Contains(key))
                {
                    return match.Groups[4].Value;
                }
                else if (match.Groups[5].Value.Contains(key))
                {
                    return match.Groups[6].Value;
                }
                else
                {
                    return match.Groups[8].Value;
                }
            }
            return "";
        }

        // Deletes given input file on local machine
        public static void DeleteFileIfExists(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        // Writes given message to the input richTextBox in a new line
        public static void WriteOutputWithNewLine(string message, RichTextBox? richTextBox)
        {
            if (richTextBox != null)
            {
                richTextBox.Invoke(richTextBox.AppendText, message + "\n");
            }

            Console.WriteLine(message);
        }

        // Writes message to input richTextBox
        public static void WriteOutput(string message, RichTextBox? richTextBox)
        {
            if (richTextBox != null)
            {
                richTextBox.Invoke(richTextBox.AppendText, message);
            }

            Console.Write(message);
        }

        // Clears RichTextBox
        public static void ClearRichTextBox(RichTextBox? richTextBox)
        {
            if (richTextBox != null)
            {
                richTextBox.Clear();
            }
        }

        // Replaces last line of richTextBox with input message
        public static void WriteOutputWithRC(string message, RichTextBox? richTextBox)
        {
            if (richTextBox != null)
            {
                richTextBox.Invoke(new Action(() =>
                {
                    string currText = richTextBox.Text;
                    richTextBox.Select(currText.LastIndexOf("\n") + 1, (currText.Length - currText.LastIndexOf("\n") + 1));
                    richTextBox.Cut();
                    richTextBox.AppendText(message);
                }));

            }
            Console.Write("\r" + message);
        }

        // Calls Kudu Command API to execute the given command on app service 
        public static KuduCommandApiResult ExecuteKuduCommandApi(string inputCommand, string ftpUsername, string ftpPassword, string appServiceName, int maxRetryCount = 3, string message = "", int timeout = 600)
        {
            if (maxRetryCount <= 0)
            {
                return new KuduCommandApiResult(Status.Failed);
            }

            string command = String.Format("bash -c \" {0} \"", inputCommand);
            var appServiceKuduCommandURL = GetKuduApiForCommandExec(appServiceName);

            int trycount = 1;
            while (trycount <= maxRetryCount)
            {
                using (var client = new HttpClient())
                {
                    try
                    {
                        client.Timeout = TimeSpan.FromSeconds(timeout);
                        var jsonString = JsonConvert.SerializeObject(new { command = command, dir = "" });
                        HttpContent httpContent = new StringContent(jsonString);
                        httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                        // Embed Status message into UserAgent field
                        if (!String.IsNullOrEmpty(message))
                        {
                            string userAgentValue = "WPMigrationTool/2.0 " + message;
                            client.DefaultRequestHeaders.Add("User-Agent", userAgentValue);
                        }

                        // Set Basic auth
                        var byteArray = Encoding.ASCII.GetBytes(ftpUsername + ":" + ftpPassword);
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, appServiceKuduCommandURL);
                        requestMessage.Content = httpContent;
                        HttpResponseMessage response = client.Send(requestMessage);
                        System.Diagnostics.Debug.WriteLine(String.Format("kudu command api statuscode is {0}; output is {1}", response.StatusCode, response.Content.ReadAsStream()));

                        // Convert response to Json
                        var responseStream = response.Content.ReadAsStream();
                        var myStreamReader = new StreamReader(responseStream, Encoding.UTF8);
                        var responseJSON = myStreamReader.ReadToEnd();
                        var responseData = JsonConvert.DeserializeObject<KuduCommandApiResponse>(responseJSON);

                        if (responseData != null && response.IsSuccessStatusCode)
                        {
                            return new KuduCommandApiResult(Status.Completed, responseData.Output, responseData.Error, responseData.ExitCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        return new KuduCommandApiResult(Status.Failed, ex.Message);
                    }

                    trycount++;
                    if (trycount > Constants.MAX_APPDATA_UPLOAD_RETRIES)
                    {
                        return new KuduCommandApiResult(Status.Failed);
                    }
                    else
                    {
                        continue;
                    }
                }
            }
            return new KuduCommandApiResult(Status.Failed);
        }

        // Clears input directory on a given app service
        public static Result ClearAppServiceDirectory(string targetFolder, string ftpUsername, string ftpPassword, string appServiceName, int maxRetryCount = Constants.MAX_APP_CLEAR_DIR_RETRIES)
        {
            Status result = Status.Failed;
            string message = "Unable to clear " + targetFolder
                + " directory on " + appServiceName + " App Service.";

            if (maxRetryCount <= 0)
            {
                return new Result(Status.Failed, message);
            }

            string listTargetDirCommand = String.Format(Constants.LIST_DIR_COMMAND, targetFolder);
            string clearTargetDirCommand = String.Format(Constants.CLEAR_APP_SERVICE_DIR_COMMAND, targetFolder);
            string createTargetDirCommand = String.Format(Constants.LIN_APP_MAKE_DIR_COMMAND, targetFolder);

            int trycount = 1;
            while (trycount <= maxRetryCount)
            {
                try
                {
                    // call Kudu Command API to delete the input directory
                    KuduCommandApiResult checkTargetDirEmptyResult = ExecuteKuduCommandApi(listTargetDirCommand, ftpUsername, ftpPassword, appServiceName, Constants.MAX_APP_CLEAR_DIR_RETRIES);
                    if (checkTargetDirEmptyResult.exitCode == 0 && String.IsNullOrEmpty(checkTargetDirEmptyResult.output))
                    {
                        result = Status.Completed;
                        message = "Successfully cleared " + targetFolder
                            + " directory on " + appServiceName + " App Service.";
                        break;
                    }

                    ExecuteKuduCommandApi(clearTargetDirCommand, ftpUsername, ftpPassword, appServiceName, Constants.MAX_APP_CLEAR_DIR_RETRIES, timeout: Constants.KUDU_API_TIMEOUT_SECONDS_LARGE);
                    ExecuteKuduCommandApi(createTargetDirCommand, ftpUsername, ftpPassword, appServiceName, Constants.MAX_RETRIES_COMMON);
                }
                catch (Exception e)
                {
                    result = Status.Failed;
                    message = "Unable to clear " + targetFolder + " directory "
                        + "on " + appServiceName + " App Service. Error=" + e.Message;
                }
                trycount++;
            }

            return new Result(result, message);
        }

        // Uploads zip file to the destination linux app service using kudu zip API
        public static Result LinuxAppServiceUploadZip(string zipFilePath, string kuduUploadUrl, string ftpUsername, string ftpPassword)
        {
            Status result = Status.Failed;
            string message = "Unable to upload " + zipFilePath + " to Linux App Service.";

            int retryCount = 1;
            while (retryCount <= Constants.MAX_APPDATA_UPLOAD_RETRIES)
            {
                using (var client = new HttpClient())
                {
                    try
                    {
                        client.DefaultRequestHeaders.Accept.Clear();
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/zip"));

                        ByteArrayContent content = new ByteArrayContent(System.IO.File.ReadAllBytes(zipFilePath));
                        content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

                        var byteArray = Encoding.ASCII.GetBytes(ftpUsername + ":" + ftpPassword);
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic",
                            Convert.ToBase64String(byteArray));

                        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Put, kuduUploadUrl);
                        requestMessage.Content = content;

                        HttpResponseMessage response = client.Send(requestMessage);
                        if (response.IsSuccessStatusCode)
                        {
                            result = Status.Completed;
                            message = "Sucessfully uploaded " + zipFilePath
                                + " to Linux App Service.";
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        result = Status.Failed;
                        message = "Unable to upload " + zipFilePath
                            + " to Linux App Service. Error=" + e.Message;
                    }

                    retryCount++;
                    if (retryCount > Constants.MAX_APPDATA_UPLOAD_RETRIES)
                    {
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
            }

            return new Result(result, message);
        }

        public static List<string> GetDefaultDropdownList(string displayMsg)
        {
            return new List<string>() { displayMsg };
        }

        // Deletes input directory on local machine
        public static void RecursiveDeleteDirectory(string targetDir)
        {
            if (!Directory.Exists(targetDir))
            {
                return;
            }
            foreach (string dir in Directory.EnumerateDirectories(targetDir))
            {
                RecursiveDeleteDirectory(dir);
            }
            Directory.Delete(targetDir, true);
        }
    }
}
