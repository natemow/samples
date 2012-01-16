using System;
using System.Collections;
using System.Data;
using System.DirectoryServices;
using System.EnterpriseServices;
using System.Runtime.InteropServices;
using System.Security.Principal;

using ad		= PBS.Connect.Business.ActiveDirectoryManager;
using common	= PBS.Connect.Business.Common;
using notify	= PBS.Connect.Business.NotificationManager;
using stations	= PBS.Connect.Business.StationDBManager;
using webboard	= PBS.Connect.Business.WebBoardDBManager;
using security	= PBS.Connect.Security.Services;

namespace PBS.Connect.Business.AccountManager
{
	#region public interface IAccountManager

	/// <summary>
	/// This interface defines methods for managing PBS Connect accounts.
	/// </summary>
	// Indicate whether a managed interface is dual, IDispatch or IUnknown based when exposed to COM
	[InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]
	public interface IAccountManager
	{
		/// <summary>
		/// Method to log user into PBS Connect application.
		/// </summary>
		/// <param name="strUsername">The User's Active Directory samAccountName</param>
		/// <param name="strPassword">The User's Active Directory password</param>
		/// <returns>bool</returns>
		bool			Login(string strUsername, string strPassword);
		
		/// <summary>
		/// Method to create a new PBS Connect account. User is added to Active Directory, SQL Stations database, and SQL WebBoard database.
		/// </summary>
		/// <param name="User">The new User object</param>
		/// <param name="Password">The new Password object</param>
		/// <returns>Common.User</returns>
		common.clsUser	Create(common.clsUser User, common.clsPassword Password);
		
		/// <summary>
		/// Method to update an existing PBS Connect account. User is updated in Active Directory, SQL Stations database, and SQL WebBoard database.
		/// </summary>
		/// <param name="User">The User object to update</param>
		/// <param name="Password">The Password object to update</param>
		/// <returns>Common.User</returns>
		common.clsUser	Update(common.clsUser User, common.clsPassword Password);
		
		/// <summary>
		/// Method to delete a PBS Connect account. User is deleted from Active Directory, SQL Stations database, and SQL WebBoard database.
		/// </summary>
		/// <param name="User">The User object to delete</param>
		/// <param name="Password">Not used</param>
		/// <returns>Common.User</returns>
		common.clsUser	Delete(common.clsUser User, common.clsPassword Password);
		
		/// <summary>
		/// Method to change the Active Directory password of a PBS Connect account. A valid OldPassword is required for successful execution.
		/// </summary>
		/// <param name="User">The User object whose Password will be changed</param>
		/// <param name="Password">The Password object (includes OldPassword, NewPassword, Hint)</param>
		/// <returns>bool</returns>
		bool			ChangePassword(common.clsUser User, common.clsPassword Password);
		
		/// <summary>
		/// Method to reset the Active Directory password of a PBS Connect account.
		/// </summary>
		/// <param name="User">The User object whose Password will be changed</param>
		/// <param name="Password">The Password object (OldPassword check is not present in this method)</param>
		/// <returns>bool</returns>
		bool			ResetPassword(common.clsUser User, common.clsPassword Password);
		
		/// <summary>
		/// Method to approve the creation of a new PBS Connect account. User record is moved from the SQL Stations.tblRegistrationRequests table to the Stations.tblStationContacts table. User is then added to Active Directory and SQL WebBoard database.
		/// </summary>
		/// <param name="User">The User to Approve</param>
		/// <param name="Password">The User's Password</param>
		/// <param name="Request">The Request object to Approve</param>
		/// <returns>Common.User</returns>
		common.clsUser	ApproveRegistration(common.clsUser User, common.clsPassword Password, common.clsRequest Request);
		
		/// <summary>
		/// Method to approve a PBS Connect User's request for access to Private Conferences in the WebBoard application.
		/// </summary>
		/// <param name="User">The User for which the ConfRequest will be Approved</param>
		/// <param name="ConfRequest">The ConfRequest object to Approve</param>
		/// <returns>bool</returns>
		bool			ApproveConfReq(common.clsUser User, common.clsConfRequest ConfRequest);
	}
	
	#endregion
	
	#region public sealed class clsAccount : ServicedComponent, IAccountManager
	
	/// <summary>
	/// This class implements the IAccountManager interface and provides the following methods/actions for managing PBS Connect accounts:
	/// Login, Create, Update, Delete, ChangePassword, ResetPassword, ApproveRegistration, and ApproveConfReq. 
	/// COM+ ServicedComponent (transaction) support is included.
	/// </summary>
	[ProgId("PBS.Connect.Business.AccountManager")]										// Specify name of serviced component
	[Description("PBS Connect Account Manager")]										// Add content to hosting COM+ App's description field
	[Transaction(TransactionOption.Required)]											// Configure component's Transaction Option
	[ObjectPooling(MinPoolSize=5,MaxPoolSize=10,CreationTimeout=2000)]					// Configure component's object pooling
	[MustRunInClientContext(false)]														// Specify COM+ Context Attributes
	[EventTrackingEnabled(true)]														// Enable event tracking
	[JustInTimeActivation(true)]														// Enable JITA for the component
	[ConstructionEnabled(Enabled=true,Default="PBS.Connect.Business.AccountManager")]	// Enable Construction String Support for the component
	[Synchronization(SynchronizationOption.Required)]									// Configure activity-based Synchronization for the component
	[GuidAttribute("9947F7BE-A6DD-423d-B719-72DF03687C1B")]								// Assign a GUID to the class
	[ClassInterface(ClassInterfaceType.AutoDispatch)]									// Indicate the type of class interface that will be generated for this class
	public sealed class clsAccount : ServicedComponent, IAccountManager
	{
		private ad.clsActiveDirectoryManager	clsADMngr;
		private common.clsUser					clsUser;
		private stations.clsStationDBManager	clsStationDBMngr;
		private webboard.WebBoardDB				clsWebBoardMngr;
		private	common.LogError					clsErr;
		static string							strMessage;
		
		#region clsAccount constructor
		/// <summary>
		/// This constructor is parameterless, but all of this class' properties must be set before instantiating the object
		/// </summary>
		public clsAccount()
		{
			if (CONFIG_CONN_StationDB == null)				{ throw new System.Exception("PBS.Connect.Business.AccountManager.CONFIG_CONN_StationDB is undefined!"); }
			if (CONFIG_CONN_WebBoardDB == null)				{ throw new System.Exception("PBS.Connect.Business.AccountManager.CONFIG_CONN_WebBoardDB is undefined!"); }
			if (CONFIG_CONN_WebBoardDefaultBoardID == 0)	{ throw new System.Exception("PBS.Connect.Business.AccountManager.CONFIG_CONN_WebBoardDefaultBoardID is undefined!"); }
			if (CONFIG_ADSI_Root == null)					{ throw new System.Exception("PBS.Connect.Business.AccountManager.CONFIG_ADSI_Root is undefined!"); }
			if (CONFIG_ADSI_AdminUsr == null)				{ throw new System.Exception("PBS.Connect.Business.AccountManager.CONFIG_ADSI_AdminUsr is undefined!"); }
			if (CONFIG_ADSI_AdminPwd == null)				{ throw new System.Exception("PBS.Connect.Business.AccountManager.CONFIG_ADSI_AdminPwd is undefined!"); }
			if (CONFIG_MAIL_SmtpServer == null)				{ throw new System.Exception("PBS.Connect.Business.AccountManager.CONFIG_MAIL_SmtpServer is undefined!"); }
			if (CONFIG_MAIL_SmtpDefaultFromAddress == null)	{ throw new System.Exception("PBS.Connect.Business.AccountManager.CONFIG_MAIL_SmtpDefaultFromAddress is undefined!"); }
			
			clsStationDBMngr			= new stations.clsStationDBManager(CONFIG_CONN_StationDB);
			clsStationDBMngr.SmtpServer	= CONFIG_MAIL_SmtpServer;
			clsWebBoardMngr				= new webboard.WebBoardDB(CONFIG_CONN_WebBoardDB);
			clsADMngr					= new ad.clsActiveDirectoryManager(CONFIG_ADSI_AdminUsr, CONFIG_ADSI_AdminPwd);
			strMessage					= "";
		}
		#endregion
		
		#region clsAccount methods
		
		/////////////////////////////////////////////////////////////////////////
		// The Following methods support core functionality required of the
		// AccountManager component and implement the IAccountManager interface
		/////////////////////////////////////////////////////////////////////////
		
		#region public bool Login(string strUsername, string strPassword)
		/// <summary>
		/// Method to log user into PBS Connect application.
		/// </summary>
		/// <param name="strUsername">The User's Active Directory samAccountName</param>
		/// <param name="strPassword">The User's Active Directory password</param>
		/// <returns>bool</returns>
		public bool Login(string strUsername, string strPassword)
		{
			#region Use native Win32 LogonUser() method to authenticate User
			/*
			// Impersonate the user.. If the call fails, then cache the impersonation
			// context so that later we can revert impersonation.
			WindowsImpersonationContext winImpContext	= null;
			bool						IsAuthenticated	= false;
			try
			{
				winImpContext = security.LogonUser.ImpersonateUser(
									CONFIG_ADSI_Root.Replace("LDAP://", ""),
									strUsername,
									strPassword,
									security.LogonType.LOGON32_LOGON_NETWORK,
									security.LogonProvider.LOGON32_PROVIDER_DEFAULT
									);
			}
			catch (ApplicationException ex)
			{
				strMessage += "<br><br>" + ex.GetBaseException().Message;
			}
			finally
			{
				if (winImpContext != null)
				{
					strMessage		+= "<br><br>Login successful";
					IsAuthenticated	 = true;
					
					// Finally we have to revert the impersonation.
					winImpContext.Undo();
				}
			}
			
			return IsAuthenticated;
			*/
			#endregion
			// or
			#region Use Active DirectoryEntry check to authenticate User
			try
			{
				DirectoryEntry de = new DirectoryEntry(CONFIG_ADSI_Root, strUsername, strPassword);
				if (de.NativeObject != null)
				{
				//	strMessage += "Login successful<br>";
					strMessage = "";
					de.Dispose();
				}
			}
			catch (System.Exception ex)
			{
				strMessage += ex.GetBaseException().Message + "<br>";
				return false;
			}
			#endregion
			
			return true;
		}
		#endregion
		
		#region [AutoComplete(true)] public common.clsUser Create(common.clsUser User, common.clsPassword Password)
		/// <summary>
		/// Method to create a new PBS Connect account. User is added to Active Directory, SQL Stations database, and SQL WebBoard database.
		/// </summary>
		/// <param name="User">The new User object</param>
		/// <param name="Password">The new Password object</param>
		/// <returns>Common.User</returns>
		[AutoComplete(true)]
		public common.clsUser Create(common.clsUser User, common.clsPassword Password)
		{
			try
			{	
				// Create Active Directory and StationContact records
				bool	boolSuccess = false;
						boolSuccess = clsADMngr.CreateADAccount(User, Password);
						boolSuccess = clsStationDBMngr.CreateProfile(User, Password);	if (!boolSuccess) { throw new System.Exception("Failed to insert STATIONS.tblSTATIONCONTACTS record!"); }
				
				clsUser = clsStationDBMngr.GetUser(User.LoginID);
				
				// Create new WebBoard account
				try
				{
					clsWebBoardMngr.wbAddUser(clsUser, CONFIG_CONN_WebBoardDefaultBoardID);
				}
				catch (System.Exception ex)
				{
					clsErr = new common.LogError();
					clsErr.WriteLogEntry(common.LogError.LogEntryType.Error, "PBSConnectBusinessWebBoardDBManager", "Unable to wbAddUser!", ex);
					throw new System.Exception(ex.GetBaseException().Message);
				}
				
				strMessage += "Account creation for \"" + User.DisplayName + "\" succeeded<br>";
				
				return clsUser;
			}
			catch (System.Exception ex)
			{
				strMessage += "Account creation failed!<ul><li>" + ex.GetBaseException().Message + "<br><br>" + ex.GetBaseException().StackTrace + "</li></ul>";
				
				clsErr = new common.LogError();
				clsErr.WriteLogEntry(common.LogError.LogEntryType.Error, "PBSConnectBusinessAccountManager", "Unable to Create account!", ex);
			}
			
			return User;
		}
		#endregion
		
		#region [AutoComplete(true)] public common.clsUser Update(common.clsUser User, common.clsPassword Password)
		/// <summary>
		/// Method to update an existing PBS Connect account. User is updated in Active Directory, SQL Stations database, and SQL WebBoard database.
		/// </summary>
		/// <param name="User">The User object to update</param>
		/// <param name="Password">The Password object to update</param>
		/// <returns>Common.User</returns>
		[AutoComplete(true)]
		public common.clsUser Update(common.clsUser User, common.clsPassword Password)
		{
			try
			{
				bool boolSuccess = false;
				
				// User opted to ChangePassword
				if (Password.OldPassword != null && Password.NewPassword != null)
				{
					boolSuccess	= this.ChangePassword(User, Password); if (!boolSuccess) { throw new System.Exception("Password change failed"); } // strMessage
				}
					// Admin opted to ResetPassword
				else if (Password.NewPassword != null && Password.Hint != null)
				{
					boolSuccess = this.ResetPassword(User, Password); if (!boolSuccess) { throw new System.Exception("Password reset failed"); } // strMessage
				}
				
				// Update Active Directory and StationContact records
				if (User.Profile.Organization.ToUpper() != "PBS")
				{
					boolSuccess	= clsADMngr.UpdateADAccount(User, Password);		if (!boolSuccess) { throw new System.Exception("Account update to Active Directory failed!"); }
				}
				boolSuccess = clsStationDBMngr.UpdateProfile(User, Password);	if (!boolSuccess) { throw new System.Exception("Account update to Stations DB failed!"); }
				
				clsUser = clsStationDBMngr.GetUser(User.LoginID);
				
				// Update WebBoard account
				clsWebBoardMngr.wbUpdateUser(clsUser);
				
				strMessage += "Account update for \"" + User.DisplayName + "\" succeeded<br>";
				
				return clsUser;
			}
			catch (System.Exception ex)
			{
				if (strMessage.Length == 0) { strMessage = "Account update failed!"; }
				
				clsErr = new common.LogError();
				clsErr.WriteLogEntry(common.LogError.LogEntryType.Error, "PBSConnectBusinessAccountManager", "Unable to Update account!", ex);
				
				throw new System.Exception(strMessage);
			}
		}
		#endregion
		
		#region [AutoComplete(true)] public common.clsUser Delete(common.clsUser User, common.clsPassword Password)
		/// <summary>
		/// Method to delete a PBS Connect account. User is deleted from Active Directory, SQL Stations database, and SQL WebBoard database.
		/// </summary>
		/// <param name="User">The User object to delete</param>
		/// <param name="Password">Not used</param>
		/// <returns>Common.User</returns>
		[AutoComplete(true)]
		public common.clsUser Delete(common.clsUser User, common.clsPassword Password)
		{
			try
			{				
				// Delete WebBoard account
				try
				{
					clsWebBoardMngr.wbDeleteUser(User);
				}
				catch (System.Exception ex)
				{
					clsErr = new common.LogError();
					clsErr.WriteLogEntry(common.LogError.LogEntryType.Error, "PBSConnectBusinessWebBoardDBManager", "Unable to wbDeleteUser!", ex);
					throw new System.Exception(ex.GetBaseException().Message);
				}
				
				// Delete Active Directory and StationContact records
				bool	boolSuccess = false;
						boolSuccess = clsADMngr.DeleteADAccount(User, Password);		if (!boolSuccess) { throw new System.Exception(); }
						boolSuccess = clsStationDBMngr.DeleteProfile(User, Password);	if (!boolSuccess) { throw new System.Exception(); }
				
				strMessage += "Account deletion for \"" + User.DisplayName + "\" succeeded<br>";
				
				return User;
			}
			catch (System.Exception ex)
			{
				strMessage += "Account deletion failed!<ul><li>" + ex.GetBaseException().Message + "<br><br>" + ex.GetBaseException().StackTrace + "</li></ul>";
				
				clsErr = new common.LogError();
				clsErr.WriteLogEntry(common.LogError.LogEntryType.Error, "PBSConnectBusinessAccountManager", "Unable to Delete account!", ex);
			}
			
			return User;
		}
		#endregion
		
		#region [AutoComplete(true)] public bool ChangePassword(common.clsUser User, common.clsPassword Password)
		/// <summary>
		/// Method to change the Active Directory password of a PBS Connect account. A valid OldPassword is required for successful execution.
		/// </summary>
		/// <param name="User">The User object whose Password will be changed</param>
		/// <param name="Password">The Password object (includes OldPassword, NewPassword, Hint)</param>
		/// <returns>bool</returns>
		[AutoComplete(true)]
		public bool ChangePassword(common.clsUser User, common.clsPassword Password)
		{
			try
			{
				// Login is successful using the old password
				if (this.Login(User.LoginID, Password.OldPassword))
				{
					// Get the full LDAP path to the user
					// Set the DirectoryEntry object for this User
					DirectoryEntry	de			= clsADMngr.GetUserByLoginID( User.LoginID );
					string			strLDAPPath	= de.Properties["distinguishedName"].Value.ToString();
					
					// Instantiate a new PBSAD object using this user path
					clsADMngr = new ad.clsActiveDirectoryManager(CONFIG_ADSI_AdminUsr, CONFIG_ADSI_AdminPwd, strLDAPPath);
					
					// Return success/failure;
					bool boolSuccess = clsADMngr.ResetPassword(User, Password);
					if (boolSuccess)
					{	
						strMessage += "Password change successful<br>";

						return true;
					}
					else
					{
						throw new System.Exception();
					}
				}
				else
				{
					throw new System.Exception();
				}
			}
			catch (System.Exception ex)
			{
			//	strMessage += "Password change failed";
				
				clsErr = new common.LogError();
				clsErr.WriteLogEntry(common.LogError.LogEntryType.Error, "PBSConnectBusinessAccountManager", "Unable to ChangePassword!", ex);
			}
			
			return false;
		}
		#endregion
		
		#region [AutoComplete(true)] public bool ResetPassword(common.clsUser User, common.clsPassword Password)
		/// <summary>
		/// Method to reset the Active Directory password of a PBS Connect account.
		/// </summary>
		/// <param name="User">The User object whose Password will be changed</param>
		/// <param name="Password">The Password object (OldPassword check is not present in this method)</param>
		/// <returns>bool</returns>
		[AutoComplete(true)]
		public bool ResetPassword(common.clsUser User, common.clsPassword Password)
		{
			try
			{
				// Get the full LDAP path to the user
				// Set the DirectoryEntry object for this User
				DirectoryEntry	de			= clsADMngr.GetUserByLoginID( User.LoginID );
				string			strLDAPPath	= de.Properties["distinguishedName"].Value.ToString();
				
				// Instantiate a new PBSAD object using this user path
				clsADMngr = new ad.clsActiveDirectoryManager(CONFIG_ADSI_AdminUsr, CONFIG_ADSI_AdminPwd, strLDAPPath);
					
				// Return success/failure;
				bool boolSuccess = clsADMngr.ResetPassword(User, Password);
				if (boolSuccess)
				{
					strMessage += "Password change successful<br>";
					
					return true;
				}
				else
				{
					throw new System.Exception();
				}
			}
			catch (System.Exception ex)
			{
				strMessage += "Password change failed!<br><br>" + ex.GetBaseException().Message + "<br>";
				
				clsErr = new common.LogError();
				clsErr.WriteLogEntry(common.LogError.LogEntryType.Error, "PBSConnectBusinessAccountManager", "Unable to ResetPassword!", ex);
			}
			
			return false;
		}
		#endregion
		
		#region [AutoComplete(true)] public common.clsUser ApproveRegistration(common.clsUser User, common.clsPassword Password, common.clsRequest Request)
		/// <summary>
		/// Method to approve the creation of a new PBS Connect account. User record is moved from the SQL Stations.tblRegistrationRequests table to the Stations.tblStationContacts table. User is then added to Active Directory and SQL WebBoard database.
		/// </summary>
		/// <param name="User">The User to Approve</param>
		/// <param name="Password">The User's Password</param>
		/// <param name="Request">The Request object to Approve</param>
		/// <returns>Common.User</returns>
		[AutoComplete(true)]
		public common.clsUser ApproveRegistration(common.clsUser User, common.clsPassword Password, common.clsRequest Request)
		{
			try
			{
				// Create Active Directory and StationContact records
				bool	boolSuccess = false;
						boolSuccess = clsADMngr.CreateADAccount(User, Password);
						if (!boolSuccess) { throw new System.Exception("Failed to create new Active Directory account!"); }
						boolSuccess = clsStationDBMngr.ApproveRegistrationRequest(Request);
						if (!boolSuccess) { throw new System.Exception("Failed to insert STATIONS.tblREGISTRATIONREQUESTS record into STATIONS.tblSTATIONCONTACTS!"); }
				
				clsUser	= clsStationDBMngr.GetUser(Request.UserInfo.LoginID);
				
				// Create new WebBoard account
				try
				{
					clsWebBoardMngr.wbAddUser(clsUser, CONFIG_CONN_WebBoardDefaultBoardID);
					if ( clsWebBoardMngr.wbGetWebBoardIdForUser(clsUser) <= 0 ) { throw new System.Exception("Failed to create new WebBoard account!"); }
				}
				catch (System.Exception ex)
				{
					clsErr = new common.LogError();
					clsErr.WriteLogEntry(common.LogError.LogEntryType.Error, "PBSConnectBusinessWebBoardDBManager", "Unable to wbAddUser!", ex);
					throw new System.Exception(ex.GetBaseException().Message);
				}
				
				strMessage += "Registration Request from \"" + Request.UserInfo.FirstName + " " + Request.UserInfo.LastName + "\" has been approved";
				
				return clsUser;
			}
			catch (System.Exception ex)
			{
				strMessage += ex.GetBaseException().Message;
				
				clsErr = new common.LogError();
				clsErr.WriteLogEntry(common.LogError.LogEntryType.Error, "PBSConnectBusinessAccountManager", "Unable to ApproveRegistration!", ex);
			}
			
			return User;
		}
		#endregion
		
		#region [AutoComplete(true)] public bool ApproveConfReq(common.clsUser User, common.clsConfRequest ConfRequest)
		/// <summary>
		/// Method to approve a PBS Connect User's request for access to Private Conferences in the WebBoard application.
		/// </summary>
		/// <param name="User">The User for which the ConfRequest will be Approved</param>
		/// <param name="ConfRequest">The ConfRequest object to Approve</param>
		/// <returns>bool</returns>
		[AutoComplete(true)]
		public bool ApproveConfReq(common.clsUser User, common.clsConfRequest ConfRequest)
		{
			try
			{
				bool	boolSuccess = false; 
						boolSuccess = clsStationDBMngr.UpdateConfRequest(ConfRequest, User); if (!boolSuccess) { throw new System.Exception("Failed to update STATIONS.tblCONFREQUEST (stationcontactid=" + ConfRequest.User.StationContactID.ToString() + ", forumid=" + ConfRequest.ForumID.ToString() + ")!"); }
				
				// Add User to WebBoard Conference
				int intWebBoardUserID = 0;
				try
				{
					intWebBoardUserID = clsWebBoardMngr.wbGetWebBoardIdForUser(ConfRequest.User);
				}
				catch
				{
					try
					{
						// User does not yet exist in WebBoard, Auto-add WebBoard account for User
						clsWebBoardMngr.wbAddUser(ConfRequest.User, CONFIG_CONN_WebBoardDefaultBoardID);
						intWebBoardUserID = clsWebBoardMngr.wbGetWebBoardIdForUser(ConfRequest.User);
					}
					catch (System.Exception ex)
					{
						clsErr = new common.LogError();
						clsErr.WriteLogEntry(common.LogError.LogEntryType.Error, "PBSConnectBusinessWebBoardDBManager", "Unable to wbAddUser!", ex);
						throw new System.Exception(ex.GetBaseException().Message);
					}
				}
				// Try to AddUserToConference
				try
				{
					clsWebBoardMngr.wbAddUserToConference(ConfRequest.User, ConfRequest.ForumID);
					strMessage += "Approve Request from \"" + ConfRequest.User.DisplayName + "\" succeeded";
				}
				catch (System.Exception ex)
				{
					clsErr = new common.LogError();
					clsErr.WriteLogEntry(common.LogError.LogEntryType.Error, "PBSConnectBusinessWebBoardDBManager", "Unable to wbAddUserToConference!", ex);
					throw new System.Exception(ex.GetBaseException().Message);
				}
				
				
				if (boolSuccess)
				{
					// Add User to corresponding Active Directory users group
					string	strUsersGroupPath = "";
					try
					{
						DataSet dsGroup;
						string	strUsersGroup = "";
						
						if (ConfRequest.ADGroup == null)
						{
							// Retrieve the AD group name for this ForumID from STATIONS.FORUMS
							DataSet ds				= clsStationDBMngr.GetForum(ConfRequest.ForumID);
									strUsersGroup	= ds.Tables[0].Rows[0]["AD_UsersGroup"].ToString();
									dsGroup			= clsADMngr.GetADDataSet(CONFIG_ADSI_Root + "/OU=Conferences,OU=AppSecurityGroups," + CONFIG_ADSI_RootLocal, "(CN=" + strUsersGroup + ")");
							
							ds.Dispose();	
						}
						else
						{
							dsGroup	= clsADMngr.GetADDataSet(CONFIG_ADSI_Root + "/OU=Conferences,OU=AppSecurityGroups," + CONFIG_ADSI_RootLocal, "(CN=" + ConfRequest.ADGroup + ")");
						}
						
						strUsersGroupPath		= dsGroup.Tables[0].Rows[0]["Path"].ToString();
						boolSuccess				= clsADMngr.AddUserToGroup( ConfRequest.User, strUsersGroupPath );
						
						if (!boolSuccess) { throw new System.Exception("Failed to add User to Active Directory group \"" + strUsersGroup + "\""); }
						
						dsGroup.Dispose();
					}
					catch (System.Exception ex)
					{
						clsErr = new common.LogError();
						clsErr.WriteLogEntry(common.LogError.LogEntryType.Error, "PBSConnectBusinessAccountManager", "Unable to add ConfRequest.User to " + strUsersGroupPath, ex);
						throw new System.Exception(ex.GetBaseException().Message);
					}
				}
				
				return boolSuccess;
			}
			catch (System.Exception ex)
			{
				strMessage += "Approve Request from \"" + ConfRequest.User.DisplayName + "\" failed!";
				
				clsErr = new common.LogError();
				clsErr.WriteLogEntry(common.LogError.LogEntryType.Error, "PBSConnectBusinessAccountManager", "Unable to ApproveConfReq!", ex);
				
				throw new System.Exception(ex.Message);
			}
		}
		#endregion
		
		#endregion
		
		#region clsAccount properties
		
		/// <summary>Returns a string message output from any actions executed in the AccountManager class</summary>
		public static string	Message { get{return strMessage;} }
		
		// Inheriting from ServicedComponent requires a constructor with no params, so
		// these fields must be set previous to any instantiation of the clsAccount object

		/// <summary>The SQL connection string for the STATIONS database</summary>
		public static string	CONFIG_CONN_StationDB;
		/// <summary>The SQL connection string for the WEBBOARD database</summary>
		public static string	CONFIG_CONN_WebBoardDB;
		/// <summary>The default WebBoard.BoardID that new Users will be added to</summary>
		public static int		CONFIG_CONN_WebBoardDefaultBoardID;
		/// <summary>The Active Directory LDAP root</summary>
		public static string	CONFIG_ADSI_Root;
		/// <summary>The Active Directory admin user (This must be an account with permission to create/update/delete AD objects)</summary>
		public static string	CONFIG_ADSI_AdminUsr;
		/// <summary>The Active Directory admin user's password</summary>
		public static string	CONFIG_ADSI_AdminPwd;
		/// <summary>The SMTP server to route mail through</summary>
		public static string	CONFIG_MAIL_SmtpServer;
		/// <summary>The SMTP address to send mail from</summary>
		public static string	CONFIG_MAIL_SmtpDefaultFromAddress;


		#region private static string CONFIG_ADSI_RootLocal
		private static string CONFIG_ADSI_RootLocal
		{
			get
			{
				// Create string "DC=pbs2,DC=pbstest,DC=com"
				string		strLDAPLocalDomain	= "";
				string[]	arrDC				= CONFIG_ADSI_Root.Replace("LDAP://","").Split(new char[]{'.'});
				IEnumerator	IEnumDC				= arrDC.GetEnumerator();
				while (IEnumDC.MoveNext())
				{
					strLDAPLocalDomain += "DC=" + IEnumDC.Current.ToString() + ",";
				}
				strLDAPLocalDomain = strLDAPLocalDomain.Remove(strLDAPLocalDomain.Length-1, 1);
				
				return strLDAPLocalDomain;
			}
		}
		#endregion
		
		#endregion
	}
	
	#endregion
}