//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using MongoDB.Bson;

namespace PDS.WITSMLstudio.Store.Security
{
    /// <summary>
    /// Represents user metadata used for authentication.
    /// </summary>
    public class DbUser
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public ObjectId Id { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        /// <value>
        /// The username.
        /// </value>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the name of the application.
        /// </summary>
        /// <value>
        /// The name of the application.
        /// </value>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Gets or sets the email.
        /// </summary>
        /// <value>
        /// The email.
        /// </value>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the comment.
        /// </summary>
        /// <value>
        /// The comment.
        /// </value>
        public string Comment { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        /// <value>
        /// The password.
        /// </value>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the password question.
        /// </summary>
        /// <value>
        /// The password question.
        /// </value>
        public string PasswordQuestion { get; set; }

        /// <summary>
        /// Gets or sets the password answer.
        /// </summary>
        /// <value>
        /// The password answer.
        /// </value>
        public string PasswordAnswer { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is approved.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is approved; otherwise, <c>false</c>.
        /// </value>
        public bool IsApproved { get; set; }

        /// <summary>
        /// Gets or sets the last activity date.
        /// </summary>
        /// <value>
        /// The last activity date.
        /// </value>
        public DateTime LastActivityDate { get; set; }

        /// <summary>
        /// Gets or sets the last login date.
        /// </summary>
        /// <value>
        /// The last login date.
        /// </value>
        public DateTime LastLoginDate { get; set; }

        /// <summary>
        /// Gets or sets the last password changed date.
        /// </summary>
        /// <value>
        /// The last password changed date.
        /// </value>
        public DateTime LastPasswordChangedDate { get; set; }

        /// <summary>
        /// Gets or sets the creation date.
        /// </summary>
        /// <value>
        /// The creation date.
        /// </value>
        public DateTime CreationDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is on line.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is on line; otherwise, <c>false</c>.
        /// </value>
        public bool IsOnLine { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is locked out.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is locked out; otherwise, <c>false</c>.
        /// </value>
        public bool IsLockedOut { get; set; }

        /// <summary>
        /// Gets or sets the last locked out date.
        /// </summary>
        /// <value>
        /// The last locked out date.
        /// </value>
        public DateTime LastLockedOutDate { get; set; }

        /// <summary>
        /// Gets or sets the failed password attempt count.
        /// </summary>
        /// <value>
        /// The failed password attempt count.
        /// </value>
        public int FailedPasswordAttemptCount { get; set; }

        /// <summary>
        /// Gets or sets the failed password attempt window start.
        /// </summary>
        /// <value>
        /// The failed password attempt window start.
        /// </value>
        public DateTime FailedPasswordAttemptWindowStart { get; set; }

        /// <summary>
        /// Gets or sets the failed password answer attempt count.
        /// </summary>
        /// <value>
        /// The failed password answer attempt count.
        /// </value>
        public int FailedPasswordAnswerAttemptCount { get; set; }

        /// <summary>
        /// Gets or sets the failed password answer attempt window start.
        /// </summary>
        /// <value>
        /// The failed password answer attempt window start.
        /// </value>
        public DateTime FailedPasswordAnswerAttemptWindowStart { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Username + " (" + Email + ") ";
        }
    }
}
