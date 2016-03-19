using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Configuration.Provider;
using System.Data;
using System.Data.Odbc;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Configuration;
using System.Web.Http;
using System.Web.Security;
using MongoDB.Driver;
using PDS.Witsml.Server.Data;

namespace PDS.Witsml.Server.Security
{
    public class MongoDbMembershipProvider : MembershipProvider
    {
        public static readonly string ProviderName = typeof(MongoDbMembershipProvider).Name;

        #region API

        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            if (ValidPassword(username, password) == false)
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }

            if (RequiresUniqueEmail && GetUserNameByEmail(email) != "")
            {
                status = MembershipCreateStatus.DuplicateEmail;
                return null;
            }

            MembershipUser u = GetUser(username, false);

            if (u == null)
            {
                var createAt = DateTime.Now;

                var usr = new DbUser();
                //usr.Id = ObjectId.NewObjectId();
                usr.Username = username;
                usr.Password = EncodePassword(password);
                usr.Email = email;
                usr.PasswordQuestion = passwordQuestion;
                usr.PasswordAnswer = EncodePassword(passwordAnswer);
                usr.IsApproved = isApproved;
                usr.CreationDate = createAt;
                usr.LastPasswordChangedDate = createAt;
                usr.LastActivityDate = createAt;
                usr.ApplicationName = ApplicationName;
                usr.IsLockedOut = false;
                usr.LastLockedOutDate = createAt;
                usr.FailedPasswordAnswerAttemptCount = 0;
                usr.FailedPasswordAnswerAttemptWindowStart = createAt;
                usr.FailedPasswordAttemptCount = 0;
                usr.FailedPasswordAttemptWindowStart = createAt;

                //Db.Add(usr);
                Collection().InsertOne(usr);
            }

            status = MembershipCreateStatus.Success;
            return GetUser(username, false);
        }

        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            var usr = ByUserName(username);

            if (usr != null && userIsOnline)
            {
                usr.LastActivityDate = DateTime.Now;
                //Db.Save(usr);
                Collection().ReplaceOne(Empty(), usr);
            }

            return ToMembershipUser(usr);
        }

        public override bool ValidateUser(string username, string password)
        {
            var usr = ByUserName(username);

            if (usr == null || usr.IsLockedOut)
                return false;

            var valid = usr.IsApproved && CheckPassword(password, usr.Password);

            if (valid)
                usr.LastLoginDate = DateTime.Now;
            else
                UpdateFailureCount(usr, FailurePassword);

            //Db.Save(usr);
            Collection().ReplaceOne(Empty(), usr);

            return valid;
        }

        public override bool ChangePassword(string username, string oldPwd, string newPwd)
        {
            if (ValidateUser(username, oldPwd) == false)
                return false;

            if (ValidPassword(username, newPwd) == false)
                throw new MembershipPasswordException("Change password canceled due to new password validation failure.");

            var usr = ByUserName(username);
            usr.Password = EncodePassword(newPwd);
            usr.LastPasswordChangedDate = DateTime.Now;
            //Db.Save(usr);
            Collection().ReplaceOne(Empty(), usr);

            return true;
        }

        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            var usr = ByUserName(username);
            //Db.Delete<DbUser>(usr);
            Collection().DeleteOne(x => x.Id == usr.Id);
            return true;
        }

        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            //var all = Db.All<DbUser>().Where(u => u.ApplicationName == ApplicationName);
            var all = Query().Where(u => u.ApplicationName == ApplicationName);
            var usrs = all.Skip(pageIndex * pageSize).Take(pageSize - 1);

            var res = new MembershipUserCollection();
            foreach (var usr in usrs)
                res.Add(ToMembershipUser(usr));

            totalRecords = all.Count();
            return res;
        }

        public override string ResetPassword(string username, string answer)
        {
            if (!EnablePasswordReset)
            {
                throw new NotSupportedException("Password reset is not enabled.");
            }

            if (answer == null && RequiresQuestionAndAnswer)
            {
                //Db.Save(UpdateFailureCount(ByUserName(username), FailurePasswordAnswer));
                Collection().ReplaceOne(Empty(), UpdateFailureCount(ByUserName(username), FailurePasswordAnswer));
                throw new ProviderException("Password answer required for password reset.");
            }

            string newPassword = Membership.GeneratePassword(newPasswordLength, MinRequiredNonAlphanumericCharacters);

            if (ValidPassword(username, newPassword) == false)
                throw new MembershipPasswordException("Reset password canceled due to password validation failure.");

            var usr = ByUserName(username);

            // User is locked ?
            if (usr.IsLockedOut)
                throw new MembershipPasswordException("The supplied user is locked out.");

            // Todo it
            //if (RequiresQuestionAndAnswer && !CheckPassword(answer, passwordAnswer))
            //{
            //   UpdateFailureCount(username, "passwordAnswer");

            //   throw new MembershipPasswordException("Incorrect password answer.");
            //}

            usr.Password = EncodePassword(newPassword);
            usr.LastPasswordChangedDate = DateTime.Now;

            //Db.Save(usr);
            Collection().InsertOne(usr);

            return newPassword;
        }

        public override string ApplicationName { get; set; }

        public override int PasswordAttemptWindow { get { return pPasswordAttemptWindow; } }

        public override int MaxInvalidPasswordAttempts { get { return pMaxInvalidPasswordAttempts; } }

        public override void Initialize(string name, NameValueCollection config)
        {
            //
            // Initialize values from web.config.
            //

            if (config == null)
                throw new ArgumentNullException("config");

            if (name == null || name.Length == 0)
                name = ProviderName;

            if (String.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", "MongoDB Membership provider");
            }

            // Initialize the abstract base class.
            base.Initialize(name, config);

            ApplicationName = GetConfigValue(config["applicationName"], System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
            pMaxInvalidPasswordAttempts = Convert.ToInt32(GetConfigValue(config["maxInvalidPasswordAttempts"], "5"));
            pPasswordAttemptWindow = Convert.ToInt32(GetConfigValue(config["passwordAttemptWindow"], "10"));
            pMinRequiredNonAlphanumericCharacters = Convert.ToInt32(GetConfigValue(config["minRequiredNonAlphanumericCharacters"], "1"));
            pMinRequiredPasswordLength = Convert.ToInt32(GetConfigValue(config["minRequiredPasswordLength"], "7"));
            pPasswordStrengthRegularExpression = Convert.ToString(GetConfigValue(config["passwordStrengthRegularExpression"], ""));
            pEnablePasswordReset = Convert.ToBoolean(GetConfigValue(config["enablePasswordReset"], "true"));
            pEnablePasswordRetrieval = Convert.ToBoolean(GetConfigValue(config["enablePasswordRetrieval"], "true"));
            pRequiresQuestionAndAnswer = Convert.ToBoolean(GetConfigValue(config["requiresQuestionAndAnswer"], "false"));
            pRequiresUniqueEmail = Convert.ToBoolean(GetConfigValue(config["requiresUniqueEmail"], "true"));
            pWriteExceptionsToEventLog = Convert.ToBoolean(GetConfigValue(config["writeExceptionsToEventLog"], "true"));

            string temp_format = config["passwordFormat"];
            if (temp_format == null)
            {
                temp_format = "Hashed";
            }

            switch (temp_format)
            {
                case "Hashed":
                    pPasswordFormat = MembershipPasswordFormat.Hashed;
                    break;
                case "Encrypted":
                    pPasswordFormat = MembershipPasswordFormat.Encrypted;
                    break;
                case "Clear":
                    pPasswordFormat = MembershipPasswordFormat.Clear;
                    break;
                default:
                    throw new ProviderException("Password format not supported.");
            }

            var resolver = GlobalConfiguration.Configuration.DependencyResolver;
            var provider = resolver.GetService(typeof(IDatabaseProvider)) as IDatabaseProvider;
            Db = provider.GetDatabase();

            //string conectionStr = string.IsNullOrWhiteSpace(Mongo.ConnectionString) ? ConfigurationManager.ConnectionStrings[config["connectionStringName"]].ConnectionString
            //    : Mongo.ConnectionString;

            //if (string.IsNullOrWhiteSpace(conectionStr))
            //    throw new ProviderException("ConnectionStringName string cannot be blank.");

            //Db = Mongo.Create(conectionStr);

            // Get encryption and decryption key information from the configuration.
            //var cfg = WebConfigurationManager.OpenWebConfiguration(System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
            machineKey = (MachineKeySection)ConfigurationManager.GetSection("system.web/machineKey");

            if (machineKey.ValidationKey.Contains("AutoGenerate"))
                if (PasswordFormat != MembershipPasswordFormat.Clear)
                    throw new ProviderException("Hashed or Encrypted passwords are not supported with auto-generated keys.");
        }

        #endregion

        #region private

        internal const string DbCollectionName = "dbUser";
        internal const string FailurePassword = "password";
        internal const string FailurePasswordAnswer = "passwordAnswer";
        internal MachineKeySection machineKey;
        internal IMongoDatabase Db;//= Mongo.Create(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString);
        int pPasswordAttemptWindow;
        int pMaxInvalidPasswordAttempts;

        private IMongoCollection<DbUser> Collection()
        {
            return Db.GetCollection<DbUser>(DbCollectionName);
        }

        private IQueryable<DbUser> Query()
        {
            return Collection().AsQueryable();
        }

        private FilterDefinition<DbUser> Empty()
        {
            return Builders<DbUser>.Filter.Empty;
        }

        DbUser UpdateFailureCount(DbUser usr, string failureType = FailurePassword)
        {
            var getFailureCount = new Func<int>(() => failureType == FailurePasswordAnswer ? usr.FailedPasswordAnswerAttemptCount : usr.FailedPasswordAttemptCount);
            var setFailureCount = new Action<int>((val) => { if (failureType == FailurePasswordAnswer) { usr.FailedPasswordAnswerAttemptCount = val; } else { usr.FailedPasswordAttemptCount = val; } });
            var getWindowStart = new Func<DateTime>(() => failureType == FailurePasswordAnswer ? usr.FailedPasswordAnswerAttemptWindowStart : usr.FailedPasswordAttemptWindowStart);
            var setWindowStart = new Action<DateTime>((val) => { if (failureType == FailurePasswordAnswer) { usr.FailedPasswordAnswerAttemptWindowStart = val; } else { usr.FailedPasswordAttemptWindowStart = val; } });

            var windowEnd = getWindowStart().AddMinutes(PasswordAttemptWindow);

            if (getFailureCount() == 0 || DateTime.Now > windowEnd)
            {
                setFailureCount(1);
                setWindowStart(DateTime.Now);
            }
            else {
                var nextFailureCount = getFailureCount() + 1;

                if (nextFailureCount >= MaxInvalidPasswordAttempts)
                {
                    usr.IsLockedOut = true;
                    usr.LastLockedOutDate = DateTime.Now;
                }
                else {
                    setFailureCount(nextFailureCount);
                }
            }

            return usr;
        }

        bool CheckPassword(string password, string dbpassword)
        {
            string pass1 = password;
            string pass2 = dbpassword;

            switch (PasswordFormat)
            {
                case MembershipPasswordFormat.Encrypted:
                    pass2 = UnEncodePassword(dbpassword);
                    break;
                case MembershipPasswordFormat.Hashed:
                    pass1 = EncodePassword(password);
                    break;
                default:
                    break;
            }

            if (pass1 == pass2)
            {
                return true;
            }

            return false;
        }

        bool ValidPassword(string username, string password)
        {
            var args = new ValidatePasswordEventArgs(username, password, true);
            OnValidatingPassword(args);
            var invalid = args.Cancel;
            return !invalid;
        }

        DbUser ByUserName(string username)
        {
            //var usr = Db.SingleOrDef<DbUser>(u => u.Username == username && u.ApplicationName == ApplicationName);
            var usr = Query().SingleOrDefault(u => u.Username == username && u.ApplicationName == ApplicationName);
            return usr;
        }

        MembershipUser ToMembershipUser(DbUser usr)
        {
            if (usr == null)
                return null;

            return new MembershipUser(this.Name, usr.Username, usr.Id.ToString(), usr.Email,
                usr.PasswordQuestion, usr.Comment, usr.IsApproved, usr.IsLockedOut,
                usr.CreationDate, usr.LastLoginDate, usr.LastActivityDate, usr.LastPasswordChangedDate,
                usr.LastLockedOutDate
            );
        }

        #endregion

        #region From MSDN example

        void UpdateFailureCount(string username, string failureType)
        {
            OdbcConnection conn = new OdbcConnection(connectionString);
            OdbcCommand cmd = new OdbcCommand("SELECT FailedPasswordAttemptCount, " +
                                                         "  FailedPasswordAttemptWindowStart, " +
                                                         "  FailedPasswordAnswerAttemptCount, " +
                                                         "  FailedPasswordAnswerAttemptWindowStart " +
                                                         "  FROM Users " +
                                                         "  WHERE Username = ? AND ApplicationName = ?", conn);

            cmd.Parameters.Add("@Username", OdbcType.VarChar, 255).Value = username;
            cmd.Parameters.Add("@ApplicationName", OdbcType.VarChar, 255).Value = ApplicationName;

            OdbcDataReader reader = null;
            DateTime windowStart = new DateTime();
            int failureCount = 0;

            try
            {
                conn.Open();

                reader = cmd.ExecuteReader(CommandBehavior.SingleRow);

                if (reader.HasRows)
                {
                    reader.Read();

                    if (failureType == "password")
                    {
                        failureCount = reader.GetInt32(0);
                        windowStart = reader.GetDateTime(1);
                    }

                    if (failureType == "passwordAnswer")
                    {
                        failureCount = reader.GetInt32(2);
                        windowStart = reader.GetDateTime(3);
                    }
                }

                reader.Close();

                DateTime windowEnd = windowStart.AddMinutes(PasswordAttemptWindow);

                if (failureCount == 0 || DateTime.Now > windowEnd)
                {
                    // First password failure or outside of PasswordAttemptWindow. 
                    // Start a new password failure count from 1 and a new window starting now.

                    if (failureType == "password")
                        cmd.CommandText = "UPDATE Users " +
                                                "  SET FailedPasswordAttemptCount = ?, " +
                                                "      FailedPasswordAttemptWindowStart = ? " +
                                                "  WHERE Username = ? AND ApplicationName = ?";

                    if (failureType == "passwordAnswer")
                        cmd.CommandText = "UPDATE Users " +
                                                "  SET FailedPasswordAnswerAttemptCount = ?, " +
                                                "      FailedPasswordAnswerAttemptWindowStart = ? " +
                                                "  WHERE Username = ? AND ApplicationName = ?";

                    cmd.Parameters.Clear();

                    cmd.Parameters.Add("@Count", OdbcType.Int).Value = 1;
                    cmd.Parameters.Add("@WindowStart", OdbcType.DateTime).Value = DateTime.Now;
                    cmd.Parameters.Add("@Username", OdbcType.VarChar, 255).Value = username;
                    cmd.Parameters.Add("@ApplicationName", OdbcType.VarChar, 255).Value = ApplicationName;

                    if (cmd.ExecuteNonQuery() < 0)
                        throw new ProviderException("Unable to update failure count and window start.");
                }
                else {
                    if (failureCount++ >= MaxInvalidPasswordAttempts)
                    {
                        // Password attempts have exceeded the failure threshold. Lock out
                        // the user.

                        cmd.CommandText = "UPDATE Users " +
                                                "  SET IsLockedOut = ?, LastLockedOutDate = ? " +
                                                "  WHERE Username = ? AND ApplicationName = ?";

                        cmd.Parameters.Clear();

                        cmd.Parameters.Add("@IsLockedOut", OdbcType.Bit).Value = true;
                        cmd.Parameters.Add("@LastLockedOutDate", OdbcType.DateTime).Value = DateTime.Now;
                        cmd.Parameters.Add("@Username", OdbcType.VarChar, 255).Value = username;
                        cmd.Parameters.Add("@ApplicationName", OdbcType.VarChar, 255).Value = ApplicationName;

                        if (cmd.ExecuteNonQuery() < 0)
                            throw new ProviderException("Unable to lock out user.");
                    }
                    else {
                        // Password attempts have not exceeded the failure threshold. Update
                        // the failure counts. Leave the window the same.

                        if (failureType == "password")
                            cmd.CommandText = "UPDATE Users " +
                                                    "  SET FailedPasswordAttemptCount = ?" +
                                                    "  WHERE Username = ? AND ApplicationName = ?";

                        if (failureType == "passwordAnswer")
                            cmd.CommandText = "UPDATE Users " +
                                                    "  SET FailedPasswordAnswerAttemptCount = ?" +
                                                    "  WHERE Username = ? AND ApplicationName = ?";

                        cmd.Parameters.Clear();

                        cmd.Parameters.Add("@Count", OdbcType.Int).Value = failureCount;
                        cmd.Parameters.Add("@Username", OdbcType.VarChar, 255).Value = username;
                        cmd.Parameters.Add("@ApplicationName", OdbcType.VarChar, 255).Value = ApplicationName;

                        if (cmd.ExecuteNonQuery() < 0)
                            throw new ProviderException("Unable to update failure count.");
                    }
                }
            }
            catch (OdbcException e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "UpdateFailureCount");

                    throw new ProviderException(exceptionMessage);
                }
                else {
                    throw e;
                }
            }
            finally
            {
                if (reader != null) { reader.Close(); }
                conn.Close();
            }
        }

        //
        // Global connection string, generated password length, generic exception message, event log info.
        //

        private int newPasswordLength = 8;
        private string eventSource = "OdbcMembershipProvider";
        private string eventLog = "Application";
        private string exceptionMessage = "An exception occurred. Please check the Event Log.";
        private string connectionString = null;

        //
        // If false, exceptions are thrown to the caller. If true,
        // exceptions are written to the event log.
        //

        private bool pWriteExceptionsToEventLog;

        public bool WriteExceptionsToEventLog
        {
            get { return pWriteExceptionsToEventLog; }
            set { pWriteExceptionsToEventLog = value; }
        }

        //
        // A helper function to retrieve config values from the configuration file.
        //

        private string GetConfigValue(string configValue, string defaultValue)
        {
            if (String.IsNullOrEmpty(configValue))
                return defaultValue;

            return configValue;
        }


        //
        // System.Web.Security.MembershipProvider properties.
        //


        private bool pEnablePasswordReset;
        private bool pEnablePasswordRetrieval;
        private bool pRequiresQuestionAndAnswer;
        private bool pRequiresUniqueEmail;
        private MembershipPasswordFormat pPasswordFormat;


        public override bool EnablePasswordReset
        {
            get { return pEnablePasswordReset; }
        }


        public override bool EnablePasswordRetrieval
        {
            get { return pEnablePasswordRetrieval; }
        }


        public override bool RequiresQuestionAndAnswer
        {
            get { return pRequiresQuestionAndAnswer; }
        }


        public override bool RequiresUniqueEmail
        {
            get { return pRequiresUniqueEmail; }
        }





        public override MembershipPasswordFormat PasswordFormat
        {
            get { return pPasswordFormat; }
        }

        private int pMinRequiredNonAlphanumericCharacters;

        public override int MinRequiredNonAlphanumericCharacters
        {
            get { return pMinRequiredNonAlphanumericCharacters; }
        }

        private int pMinRequiredPasswordLength;

        public override int MinRequiredPasswordLength
        {
            get { return pMinRequiredPasswordLength; }
        }

        private string pPasswordStrengthRegularExpression;

        public override string PasswordStrengthRegularExpression
        {
            get { return pPasswordStrengthRegularExpression; }
        }

        //
        // MembershipProvider.ChangePasswordQuestionAndAnswer
        //

        public override bool ChangePasswordQuestionAndAnswer(string username,
                          string password,
                          string newPwdQuestion,
                          string newPwdAnswer)
        {
            if (!ValidateUser(username, password))
                return false;

            OdbcConnection conn = new OdbcConnection(connectionString);
            OdbcCommand cmd = new OdbcCommand("UPDATE Users " +
                      " SET PasswordQuestion = ?, PasswordAnswer = ?" +
                      " WHERE Username = ? AND ApplicationName = ?", conn);

            cmd.Parameters.Add("@Question", OdbcType.VarChar, 255).Value = newPwdQuestion;
            cmd.Parameters.Add("@Answer", OdbcType.VarChar, 255).Value = EncodePassword(newPwdAnswer);
            cmd.Parameters.Add("@Username", OdbcType.VarChar, 255).Value = username;
            cmd.Parameters.Add("@ApplicationName", OdbcType.VarChar, 255).Value = ApplicationName;


            int rowsAffected = 0;

            try
            {
                conn.Open();

                rowsAffected = cmd.ExecuteNonQuery();
            }
            catch (OdbcException e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "ChangePasswordQuestionAndAnswer");

                    throw new ProviderException(exceptionMessage);
                }
                else {
                    throw e;
                }
            }
            finally
            {
                conn.Close();
            }

            if (rowsAffected > 0)
            {
                return true;
            }

            return false;
        }



        //
        // MembershipProvider.GetNumberOfUsersOnline
        //

        public override int GetNumberOfUsersOnline()
        {

            TimeSpan onlineSpan = new TimeSpan(0, System.Web.Security.Membership.UserIsOnlineTimeWindow, 0);
            DateTime compareTime = DateTime.Now.Subtract(onlineSpan);

            OdbcConnection conn = new OdbcConnection(connectionString);
            OdbcCommand cmd = new OdbcCommand("SELECT Count(*) FROM Users " +
                      " WHERE LastActivityDate > ? AND ApplicationName = ?", conn);

            cmd.Parameters.Add("@CompareDate", OdbcType.DateTime).Value = compareTime;
            cmd.Parameters.Add("@ApplicationName", OdbcType.VarChar, 255).Value = ApplicationName;

            int numOnline = 0;

            try
            {
                conn.Open();

                numOnline = (int)cmd.ExecuteScalar();
            }
            catch (OdbcException e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "GetNumberOfUsersOnline");

                    throw new ProviderException(exceptionMessage);
                }
                else {
                    throw e;
                }
            }
            finally
            {
                conn.Close();
            }

            return numOnline;
        }



        //
        // MembershipProvider.GetPassword
        //

        public override string GetPassword(string username, string answer)
        {
            if (!EnablePasswordRetrieval)
            {
                throw new ProviderException("Password Retrieval Not Enabled.");
            }

            if (PasswordFormat == MembershipPasswordFormat.Hashed)
            {
                throw new ProviderException("Cannot retrieve Hashed passwords.");
            }

            OdbcConnection conn = new OdbcConnection(connectionString);
            OdbcCommand cmd = new OdbcCommand("SELECT Password, PasswordAnswer, IsLockedOut FROM Users " +
                    " WHERE Username = ? AND ApplicationName = ?", conn);

            cmd.Parameters.Add("@Username", OdbcType.VarChar, 255).Value = username;
            cmd.Parameters.Add("@ApplicationName", OdbcType.VarChar, 255).Value = ApplicationName;

            string password = "";
            string passwordAnswer = "";
            OdbcDataReader reader = null;

            try
            {
                conn.Open();

                reader = cmd.ExecuteReader(CommandBehavior.SingleRow);

                if (reader.HasRows)
                {
                    reader.Read();

                    if (reader.GetBoolean(2))
                        throw new MembershipPasswordException("The supplied user is locked out.");

                    password = reader.GetString(0);
                    passwordAnswer = reader.GetString(1);
                }
                else {
                    throw new MembershipPasswordException("The supplied user name is not found.");
                }
            }
            catch (OdbcException e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "GetPassword");

                    throw new ProviderException(exceptionMessage);
                }
                else {
                    throw e;
                }
            }
            finally
            {
                if (reader != null) { reader.Close(); }
                conn.Close();
            }


            if (RequiresQuestionAndAnswer && !CheckPassword(answer, passwordAnswer))
            {
                UpdateFailureCount(username, "passwordAnswer");

                throw new MembershipPasswordException("Incorrect password answer.");
            }


            if (PasswordFormat == MembershipPasswordFormat.Encrypted)
            {
                password = UnEncodePassword(password);
            }

            return password;
        }




        //
        // MembershipProvider.GetUser(object, bool)
        //

        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            OdbcConnection conn = new OdbcConnection(connectionString);
            OdbcCommand cmd = new OdbcCommand("SELECT PKID, Username, Email, PasswordQuestion," +
                    " Comment, IsApproved, IsLockedOut, CreationDate, LastLoginDate," +
                    " LastActivityDate, LastPasswordChangedDate, LastLockedOutDate" +
                    " FROM Users WHERE PKID = ?", conn);

            cmd.Parameters.Add("@PKID", OdbcType.UniqueIdentifier).Value = providerUserKey;

            MembershipUser u = null;
            OdbcDataReader reader = null;

            try
            {
                conn.Open();

                reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    reader.Read();
                    u = GetUserFromReader(reader);

                    if (userIsOnline)
                    {
                        OdbcCommand updateCmd = new OdbcCommand("UPDATE Users " +
                                     "SET LastActivityDate = ? " +
                                     "WHERE PKID = ?", conn);

                        updateCmd.Parameters.Add("@LastActivityDate", OdbcType.DateTime).Value = DateTime.Now;
                        updateCmd.Parameters.Add("@PKID", OdbcType.UniqueIdentifier).Value = providerUserKey;

                        updateCmd.ExecuteNonQuery();
                    }
                }

            }
            catch (OdbcException e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "GetUser(Object, Boolean)");

                    throw new ProviderException(exceptionMessage);
                }
                else {
                    throw e;
                }
            }
            finally
            {
                if (reader != null) { reader.Close(); }

                conn.Close();
            }

            return u;
        }


        //
        // GetUserFromReader
        //    A helper function that takes the current row from the OdbcDataReader
        // and hydrates a MembershiUser from the values. Called by the 
        // MembershipUser.GetUser implementation.
        //

        private MembershipUser GetUserFromReader(OdbcDataReader reader)
        {
            object providerUserKey = reader.GetValue(0);
            string username = reader.GetString(1);
            string email = reader.GetString(2);

            string passwordQuestion = "";
            if (reader.GetValue(3) != DBNull.Value)
                passwordQuestion = reader.GetString(3);

            string comment = "";
            if (reader.GetValue(4) != DBNull.Value)
                comment = reader.GetString(4);

            bool isApproved = reader.GetBoolean(5);
            bool isLockedOut = reader.GetBoolean(6);
            DateTime creationDate = reader.GetDateTime(7);

            DateTime lastLoginDate = new DateTime();
            if (reader.GetValue(8) != DBNull.Value)
                lastLoginDate = reader.GetDateTime(8);

            DateTime lastActivityDate = reader.GetDateTime(9);
            DateTime lastPasswordChangedDate = reader.GetDateTime(10);

            DateTime lastLockedOutDate = new DateTime();
            if (reader.GetValue(11) != DBNull.Value)
                lastLockedOutDate = reader.GetDateTime(11);

            MembershipUser u = new MembershipUser(this.Name,
                                                              username,
                                                              providerUserKey,
                                                              email,
                                                              passwordQuestion,
                                                              comment,
                                                              isApproved,
                                                              isLockedOut,
                                                              creationDate,
                                                              lastLoginDate,
                                                              lastActivityDate,
                                                              lastPasswordChangedDate,
                                                              lastLockedOutDate);

            return u;
        }


        //
        // MembershipProvider.UnlockUser
        //

        public override bool UnlockUser(string username)
        {
            OdbcConnection conn = new OdbcConnection(connectionString);
            OdbcCommand cmd = new OdbcCommand("UPDATE Users " +
                                                         " SET IsLockedOut = False, LastLockedOutDate = ? " +
                                                         " WHERE Username = ? AND ApplicationName = ?", conn);

            cmd.Parameters.Add("@LastLockedOutDate", OdbcType.DateTime).Value = DateTime.Now;
            cmd.Parameters.Add("@Username", OdbcType.VarChar, 255).Value = username;
            cmd.Parameters.Add("@ApplicationName", OdbcType.VarChar, 255).Value = ApplicationName;

            int rowsAffected = 0;

            try
            {
                conn.Open();

                rowsAffected = cmd.ExecuteNonQuery();
            }
            catch (OdbcException e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "UnlockUser");

                    throw new ProviderException(exceptionMessage);
                }
                else {
                    throw e;
                }
            }
            finally
            {
                conn.Close();
            }

            if (rowsAffected > 0)
                return true;

            return false;
        }


        //
        // MembershipProvider.GetUserNameByEmail
        //

        public override string GetUserNameByEmail(string email)
        {
            OdbcConnection conn = new OdbcConnection(connectionString);
            OdbcCommand cmd = new OdbcCommand("SELECT Username" +
                    " FROM Users WHERE Email = ? AND ApplicationName = ?", conn);

            cmd.Parameters.Add("@Email", OdbcType.VarChar, 128).Value = email;
            cmd.Parameters.Add("@ApplicationName", OdbcType.VarChar, 255).Value = ApplicationName;

            string username = "";

            try
            {
                conn.Open();

                username = (string)cmd.ExecuteScalar();
            }
            catch (OdbcException e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "GetUserNameByEmail");

                    throw new ProviderException(exceptionMessage);
                }
                else {
                    throw e;
                }
            }
            finally
            {
                conn.Close();
            }

            if (username == null)
                username = "";

            return username;
        }






        //
        // MembershipProvider.UpdateUser
        //

        public override void UpdateUser(MembershipUser user)
        {
            OdbcConnection conn = new OdbcConnection(connectionString);
            OdbcCommand cmd = new OdbcCommand("UPDATE Users " +
                      " SET Email = ?, Comment = ?," +
                      " IsApproved = ?" +
                      " WHERE Username = ? AND ApplicationName = ?", conn);

            cmd.Parameters.Add("@Email", OdbcType.VarChar, 128).Value = user.Email;
            cmd.Parameters.Add("@Comment", OdbcType.VarChar, 255).Value = user.Comment;
            cmd.Parameters.Add("@IsApproved", OdbcType.Bit).Value = user.IsApproved;
            cmd.Parameters.Add("@Username", OdbcType.VarChar, 255).Value = user.UserName;
            cmd.Parameters.Add("@ApplicationName", OdbcType.VarChar, 255).Value = ApplicationName;


            try
            {
                conn.Open();

                cmd.ExecuteNonQuery();
            }
            catch (OdbcException e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "UpdateUser");

                    throw new ProviderException(exceptionMessage);
                }
                else {
                    throw e;
                }
            }
            finally
            {
                conn.Close();
            }
        }






        //
        // CheckPassword
        //   Compares password values based on the MembershipPasswordFormat.
        //



        //
        // EncodePassword
        //   Encrypts, Hashes, or leaves the password clear based on the PasswordFormat.
        //

        private string EncodePassword(string password)
        {
            if (password == null)
                return null;

            string encodedPassword = password;

            switch (PasswordFormat)
            {
                case MembershipPasswordFormat.Clear:
                    break;
                case MembershipPasswordFormat.Encrypted:
                    encodedPassword =
                      Convert.ToBase64String(EncryptPassword(Encoding.Unicode.GetBytes(password)));
                    break;
                case MembershipPasswordFormat.Hashed:
                    HMACSHA1 hash = new HMACSHA1();
                    hash.Key = HexToByte(machineKey.ValidationKey);
                    encodedPassword =
                      Convert.ToBase64String(hash.ComputeHash(Encoding.Unicode.GetBytes(password)));
                    break;
                default:
                    throw new ProviderException("Unsupported password format.");
            }

            return encodedPassword;
        }


        //
        // UnEncodePassword
        //   Decrypts or leaves the password clear based on the PasswordFormat.
        //

        private string UnEncodePassword(string encodedPassword)
        {
            string password = encodedPassword;

            switch (PasswordFormat)
            {
                case MembershipPasswordFormat.Clear:
                    break;
                case MembershipPasswordFormat.Encrypted:
                    password =
                      Encoding.Unicode.GetString(DecryptPassword(Convert.FromBase64String(password)));
                    break;
                case MembershipPasswordFormat.Hashed:
                    throw new ProviderException("Cannot unencode a hashed password.");
                default:
                    throw new ProviderException("Unsupported password format.");
            }

            return password;
        }

        //
        // HexToByte
        //   Converts a hexadecimal string to a byte array. Used to convert encryption
        // key values from the configuration.
        //

        private byte[] HexToByte(string hexString)
        {
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }


        //
        // MembershipProvider.FindUsersByName
        //

        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {

            OdbcConnection conn = new OdbcConnection(connectionString);
            OdbcCommand cmd = new OdbcCommand("SELECT Count(*) FROM Users " +
                         "WHERE Username LIKE ? AND ApplicationName = ?", conn);
            cmd.Parameters.Add("@UsernameSearch", OdbcType.VarChar, 255).Value = usernameToMatch;
            cmd.Parameters.Add("@ApplicationName", OdbcType.VarChar, 255).Value = ApplicationName;

            MembershipUserCollection users = new MembershipUserCollection();

            OdbcDataReader reader = null;

            try
            {
                conn.Open();
                totalRecords = (int)cmd.ExecuteScalar();

                if (totalRecords <= 0) { return users; }

                cmd.CommandText = "SELECT PKID, Username, Email, PasswordQuestion," +
                  " Comment, IsApproved, IsLockedOut, CreationDate, LastLoginDate," +
                  " LastActivityDate, LastPasswordChangedDate, LastLockedOutDate " +
                  " FROM Users " +
                  " WHERE Username LIKE ? AND ApplicationName = ? " +
                  " ORDER BY Username Asc";

                reader = cmd.ExecuteReader();

                int counter = 0;
                int startIndex = pageSize * pageIndex;
                int endIndex = startIndex + pageSize - 1;

                while (reader.Read())
                {
                    if (counter >= startIndex)
                    {
                        MembershipUser u = GetUserFromReader(reader);
                        users.Add(u);
                    }

                    if (counter >= endIndex) { cmd.Cancel(); }

                    counter++;
                }
            }
            catch (OdbcException e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "FindUsersByName");

                    throw new ProviderException(exceptionMessage);
                }
                else {
                    throw e;
                }
            }
            finally
            {
                if (reader != null) { reader.Close(); }

                conn.Close();
            }

            return users;
        }

        //
        // MembershipProvider.FindUsersByEmail
        //

        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            OdbcConnection conn = new OdbcConnection(connectionString);
            OdbcCommand cmd = new OdbcCommand("SELECT Count(*) FROM Users " +
                                                         "WHERE Email LIKE ? AND ApplicationName = ?", conn);
            cmd.Parameters.Add("@EmailSearch", OdbcType.VarChar, 255).Value = emailToMatch;
            cmd.Parameters.Add("@ApplicationName", OdbcType.VarChar, 255).Value = ApplicationName;

            MembershipUserCollection users = new MembershipUserCollection();

            OdbcDataReader reader = null;
            totalRecords = 0;

            try
            {
                conn.Open();
                totalRecords = (int)cmd.ExecuteScalar();

                if (totalRecords <= 0) { return users; }

                cmd.CommandText = "SELECT PKID, Username, Email, PasswordQuestion," +
                            " Comment, IsApproved, IsLockedOut, CreationDate, LastLoginDate," +
                            " LastActivityDate, LastPasswordChangedDate, LastLockedOutDate " +
                            " FROM Users " +
                            " WHERE Email LIKE ? AND ApplicationName = ? " +
                            " ORDER BY Username Asc";

                reader = cmd.ExecuteReader();

                int counter = 0;
                int startIndex = pageSize * pageIndex;
                int endIndex = startIndex + pageSize - 1;

                while (reader.Read())
                {
                    if (counter >= startIndex)
                    {
                        MembershipUser u = GetUserFromReader(reader);
                        users.Add(u);
                    }

                    if (counter >= endIndex) { cmd.Cancel(); }

                    counter++;
                }
            }
            catch (OdbcException e)
            {
                if (WriteExceptionsToEventLog)
                {
                    WriteToEventLog(e, "FindUsersByEmail");

                    throw new ProviderException(exceptionMessage);
                }
                else {
                    throw e;
                }
            }
            finally
            {
                if (reader != null) { reader.Close(); }

                conn.Close();
            }

            return users;
        }


        //
        // WriteToEventLog
        //   A helper function that writes exception detail to the event log. Exceptions
        // are written to the event log as a security measure to avoid private database
        // details from being returned to the browser. If a method does not return a status
        // or boolean indicating the action succeeded or failed, a generic exception is also 
        // thrown by the caller.
        //

        private void WriteToEventLog(Exception e, string action)
        {
            EventLog log = new EventLog();
            log.Source = eventSource;
            log.Log = eventLog;

            string message = "An exception occurred communicating with the data source.\n\n";
            message += "Action: " + action + "\n\n";
            message += "Exception: " + e.ToString();

            log.WriteEntry(message);
        }

        #endregion
    }
}
