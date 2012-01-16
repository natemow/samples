using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.DirectoryServices;
using System.EnterpriseServices;
using System.Runtime.InteropServices;
using System.Text;

using common = PBS.Connect.Business.Common;

namespace PBS.Connect.Business.ActiveDirectoryManager
{
	#region enum ADS_USER_FLAG_ENUM : long
	/// <summary>
	/// See http://msdn.microsoft.com/library/default.asp?url=/library/en-us/adsi/adsi/ads_user_flag_enum.asp
	/// for detailed descriptions of each of the ADS_USER_FLAGS 
	/// </summary>
	enum ADS_USER_FLAG_ENUM : long
	{
		ADS_UF_SCRIPT									= 1,		// 0x1
		ADS_UF_ACCOUNTDISABLE							= 2,		// 0x2
		ADS_UF_HOMEDIR_REQUIRED							= 8,		// 0x8
		ADS_UF_LOCKOUT									= 16,		// 0x10
		ADS_UF_PASSWD_NOTREQD							= 32,		// 0x20
		ADS_UF_PASSWD_CANT_CHANGE						= 64,		// 0x40
		ADS_UF_ENCRYPTED_TEXT_PASSWORD_ALLOWED			= 128,		// 0x80
		ADS_UF_TEMP_DUPLICATE_ACCOUNT					= 256,		// 0x100
		ADS_UF_NORMAL_ACCOUNT							= 512,		// 0x200
		ADS_UF_INTERDOMAIN_TRUST_ACCOUNT				= 2048,		// 0x800
		ADS_UF_WORKSTATION_TRUST_ACCOUNT				= 4096,		// 0x1000
		ADS_UF_SERVER_TRUST_ACCOUNT						= 8192,		// 0x2000
		ADS_UF_DONT_EXPIRE_PASSWD						= 65536,	// 0x10000
		ADS_UF_MNS_LOGON_ACCOUNT						= 131072,	// 0x20000
		ADS_UF_SMARTCARD_REQUIRED						= 262144,	// 0x40000
		ADS_UF_TRUSTED_FOR_DELEGATION					= 524288,	// 0x80000
		ADS_UF_NOT_DELEGATED							= 1048576,	// 0x100000
		ADS_UF_USE_DES_KEY_ONLY							= 2097152,	// 0x200000
		ADS_UF_DONT_REQUIRE_PREAUTH						= 4194304,	// 0x400000
		ADS_UF_PASSWORD_EXPIRED							= 8388608,	// 0x800000
		ADS_UF_TRUSTED_TO_AUTHENTICATE_FOR_DELEGATION	= 16777216	// 0x1000000
	}
	#endregion
	
	#region sealed class clsPBSActiveDirectory
	sealed class clsPBSActiveDirectory
	{
		string			strLDAPPath;
		string			strLDAPLocalDomain;
		string			strAdminUsr;
		string			strAdminPwd;
		DirectoryEntry	de;
		common.LogError	clsErr;
		
		#region clsPBSActiveDirectory constructor
		
		public clsPBSActiveDirectory()
		{
			this.strLDAPPath	= CONFIG_ADSI_Root;
			this.strAdminUsr	= CONFIG_ADSI_AdminUsr;
			this.strAdminPwd	= CONFIG_ADSI_AdminPwd;
			
			// Create string "DC=pbs2,DC=pbstest,DC=com"
			strLDAPLocalDomain	= "";
			string[]	arrDC	= strLDAPPath.Replace("LDAP://","").Split(new char[]{'.'});
			IEnumerator	IEnumDC	= arrDC.GetEnumerator();
			while (IEnumDC.MoveNext())
			{
				strLDAPLocalDomain += "DC=" + IEnumDC.Current.ToString() + ",";
			}
			this.strLDAPLocalDomain = strLDAPLocalDomain.Remove(strLDAPLocalDomain.Length-1, 1);
			
			
			if (CONFIG_ADSI_DistinguishedName != null) { this.strLDAPPath += "/" + CONFIG_ADSI_DistinguishedName; }
		}
		
		#endregion
		
		#region clsPBSActiveDirectory methods
		
		#region  public bool CreateADAccount(common.clsUser User, common.clsPassword Password)
		public bool CreateADAccount(common.clsUser User, common.clsPassword Password)
		{
			this.strLDAPPath					+= "/OU=" + User.Profile.Organization + ",OU=MemberStations," + strLDAPLocalDomain;
			DirectoryEntry	deGroupOU			 = this.GetUserByLoginID(User.Profile.Organization + "Usr");
			DirectoryEntry	deGroupPBSCUsers	 = this.GetUserByLoginID("PBSC All Users");
			
			// Add User to OU=[MemberStation]
			// Set User account properties
			// Set User account password
			// Set User account = Enabled
			// Add User to [MemberStation]Usr Group (Use the pre-W2K AD group name!!!!!!!!!)
			// Add User to "PBSC All Users" Group
			bool	boolSuccess = false;
					boolSuccess = this.AddToOU(User, strLDAPPath);					if (!boolSuccess) { return false; }
					boolSuccess = this.UpdateADAccount(User, Password);				if (!boolSuccess) { return false; }
					boolSuccess = this.ResetPassword(User, Password);				if (!boolSuccess) { return false; }
					boolSuccess = this.EnableADAccount(User);						if (!boolSuccess) { return false; }
					boolSuccess = this.AddUserToGroup(User, deGroupOU.Path);		if (!boolSuccess) { return false; }
			//		boolSuccess = this.AddUserToGroup(User, deGroupPBSCUsers.Path);	if (!boolSuccess) { return false; }
					
			deGroupOU.Dispose();
			deGroupPBSCUsers.Dispose();
			
			return boolSuccess;
		}
		#endregion
		
		#region  public bool UpdateADAccount(common.clsUser User, common.clsPassword Password)
		public bool UpdateADAccount(common.clsUser User, common.clsPassword Password)
		{
			try
			{
				de = this.GetUserByLoginID(User.LoginID);
			
				// Update the DirectoryEntry property collection for this User
				de.Properties["samAccountName"].Value			= User.LoginID;
				de.Properties["userPrincipalName"].Value		= User.LoginID + "@" + ConfigurationSettings.AppSettings["ActiveDirectoryRootString"].Replace("LDAP://","");
				de.Properties["givenName"].Value				= User.Profile.FirstName;
				de.Properties["sn"].Value						= User.Profile.LastName;
				de.Properties["displayName"].Value				= User.DisplayName;
				
				//string	strInitials	 = User.Profile.FirstName.Substring(0,1);
				//if (!common.clsFunctions.IsFieldEmpty(User.Profile.MiddleName)) { strInitials += User.Profile.MiddleName.Substring(0,1); }
				//strInitials += User.Profile.LastName.Substring(0,1);
				//de.Properties["initials"].Value					= strInitials;
				
				de.Properties["company"].Value					= User.Profile.Organization;
				
			//	de.Properties["title"].Value					= User.Profile.Title;
			//	de.Properties["description"].Value				= User.Profile.JobFunction;
			//	de.Properties["streetAddress"].Value			= User.Profile.Address1 + ", " + User.Profile.Address2;
			//	de.Properties["l"].Value						= User.Profile.City;
			//	de.Properties["st"].Value						= User.Profile.State;
			//	de.Properties["postalCode"].Value				= User.Profile.Zip;
				
			//	de.Properties["c"].Value						= "US";
			//	de.Properties["co"].Value						= "UNITED STATES";
			//	de.Properties["countryCode"].Value				= 840;
				/*
				string strTelephone = "";
				
				bool	boolExtExists	= true;
				if (User.Profile.Extension == null) { boolExtExists = false; }
				if (User.Profile.Extension.Length.Equals(0)) { boolExtExists = false; }
				
				if (boolExtExists) 
				{
					strTelephone = User.Profile.AreaCode + " " + User.Profile.Exchange + " ext. " + User.Profile.Extension;
				}
				else 
				{
					strTelephone = User.Profile.AreaCode + " " + User.Profile.Exchange;
				}
				de.Properties["telephoneNumber"].Value			= strTelephone;
				de.Properties["facsimileTelephoneNumber"].Value	= User.Profile.FaxAreaCode + " " + User.Profile.FaxExchange;
				*/
				de.Properties["mail"].Value						= User.Profile.EmailAddress;
				
				/*
				CDOEXM.IMailRecipient mailUser = (CDOEXM.IMailRecipient) de.NativeObject;
				try		{ mailUser.MailDisable(); }
				catch	{ }
				mailUser.MailEnable( "SMTP:" + User.Profile.EmailAddress );
				*/
				/*
				if (de.Properties.Contains("proxyAddresses"))
				{
					de.Properties["proxyAddresses"].Clear();
					de.Properties["proxyAddresses"].Add( "SMTP:"+User.Profile.EmailAddress );
					de.Properties["proxyAddresses"].Add( "X400:c=us;a= ;p=company US;o=Exchange;s="+User.Profile.LastName+";g="+User.DisplayName+";" );
				}
				if (de.Properties.Contains("mailNickname"))
				{
					de.Properties["mailNickname"].Value		= User.LoginID;
				}
				if (de.Properties.Contains("targetAddress"))
				{
					de.Properties["targetAddress"].Value	= "SMTP:"+User.Profile.EmailAddress;
				}
				*/
				
				// Commit changes, refresh cache
				de.CommitChanges();
				de.RefreshCache();
				
				return true;
			}
			catch (System.Exception ex)
			{
				clsErr = new common.LogError();
				clsErr.WriteLogEntry(common.LogError.LogEntryType.Error, "PBSConnectBusinessActiveDirectoryManager", "Unable to UpdateADAccount! " + strLDAPPath + " ", ex);
				
				return false;
			}
		}
		#endregion
		
		#region  public bool DeleteADAccount(common.clsUser User, common.clsPassword Password)
		public bool DeleteADAccount(common.clsUser User, common.clsPassword Password)
		{
			// Get the DirectoryEntry for the supplied MemberStation OU
			strLDAPPath += "/OU=" + User.Profile.Organization + ",OU=MemberStations," + strLDAPLocalDomain;
			
			try
			{
				de = new DirectoryEntry(strLDAPPath, strAdminUsr, strAdminPwd);
				
				// Get the DirectoryEntry for this User
				DirectorySearcher	srch = new DirectorySearcher(strLDAPPath);
				srch.Filter	= "(samAccountName=" + User.LoginID + ")";
				srch.PropertiesToLoad.Add("CN");
				SearchResult		srchResult	= srch.FindOne();
				DirectoryEntry		deUser		= srchResult.GetDirectoryEntry();
				
				// Remove this User from OU container
				de.Children.Remove( deUser );
				
				deUser.Dispose();
				srch.Dispose();
				de.CommitChanges();
				de.RefreshCache();
				
				return true;
			}
			catch (System.Exception ex)
			{
				clsErr = new common.LogError();
				clsErr.WriteLogEntry(common.LogError.LogEntryType.Error, "PBSConnectBusinessActiveDirectoryManager", "Unable to DeleteADAccount! " + strLDAPPath + " ", ex);
				
				return false;
			}
		}
		#endregion
		
		#region  public bool ResetPassword(common.clsUser User, common.clsPassword Password)
		public bool ResetPassword(common.clsUser User, common.clsPassword Password)
		{
			try
			{
				this.strLDAPPath = this.GetUserByLoginID(User.LoginID).Path.Replace("LDAP://", CONFIG_ADSI_Root + "/");
				
				// Add NewPassword to an array
				string		strNewPwd	= Password.NewPassword;
				object[]	arrPwd		= new object[1];
				arrPwd.SetValue(strNewPwd, 0);
				
				// Get the DirectoryEntry for this User, authenticating with the Admin usr/pwd
				de = new DirectoryEntry(strLDAPPath, strAdminUsr, strAdminPwd);
				
				// Call native AD method to set new Password
				de.Invoke("setPassword", arrPwd);
				de.CommitChanges();
				de.RefreshCache();
				
				return true;
			}
			catch (System.Exception ex)
			{
				clsErr = new common.LogError();
				clsErr.WriteLogEntry(common.LogError.LogEntryType.Error, "PBSConnectBusinessActiveDirectoryManager", "Unable to ResetPassword! " + strLDAPPath + " ", ex);
				
				return false;
			}			
		}
		#endregion
		
		// Note that these methods use bitwise operators to interact with 
		// the ADS_USER_FLAG_ENUM and require the "unsafe" flag
		// (Consequently, the entire assembly must be compiled using the /unsafe switch)
		
		#region  unsafe public bool EnableADAccount(common.clsUser User)
		unsafe public bool EnableADAccount(common.clsUser User)
		{
			try
			{
				DirectoryEntry	deUser	= this.GetUserByLoginID(User.LoginID);
				int				intVal	= (int) deUser.Properties["userAccountControl"].Value;
				
				deUser.Properties["userAccountControl"].Value	= intVal & (int) ~ADS_USER_FLAG_ENUM.ADS_UF_ACCOUNTDISABLE;
				deUser.CommitChanges();
				deUser.RefreshCache();
				deUser.Dispose();
				
				// Return success/failure
				return this.IsADAccountEnabled(User);
			}
			catch (System.Exception ex)
			{
				clsErr = new common.LogError();
				clsErr.WriteLogEntry(common.LogError.LogEntryType.Error, "PBSConnectBusinessActiveDirectoryManager", "Unable to EnableADAccount! " + strLDAPPath + " ", ex);
				
				return false;
			}
		}
		#endregion
		
		#region  unsafe public bool DisableADAccount(common.clsUser User)
		unsafe public bool DisableADAccount(common.clsUser User)
		{
			try
			{
				DirectoryEntry	deUser	= this.GetUserByLoginID(User.LoginID);
				int				intVal	= (int) deUser.Properties["userAccountControl"].Value;
				
				deUser.Properties["userAccountControl"].Value	= intVal | (int) ADS_USER_FLAG_ENUM.ADS_UF_ACCOUNTDISABLE;
				deUser.CommitChanges();
				deUser.RefreshCache();
				
				// Return success/failure
				return !this.IsADAccountEnabled(User);
			}
			catch (System.Exception ex)
			{
				clsErr = new common.LogError();
				clsErr.WriteLogEntry(common.LogError.LogEntryType.Error, "PBSConnectBusinessActiveDirectoryManager", "Unable to DisableADAccount! " + strLDAPPath + " ", ex);
				
				return false;
			}
		}
		#endregion
		
		#region  unsafe public bool IsADAccountEnabled(common.clsUser User)
		
		unsafe public bool IsADAccountEnabled(common.clsUser User)
		{
			DirectoryEntry	deUser	= this.GetUserByLoginID(User.LoginID);
			int				intVal	= (int) deUser.Properties["userAccountControl"].Value;
			
			return !deUser.Properties["userAccountControl"].Value.Equals(intVal | (int) ADS_USER_FLAG_ENUM.ADS_UF_ACCOUNTDISABLE);
		}
		#endregion
		
		
		
		#region public bool AddUserToGroup(common.clsUser User, string strLDAPPath)
		public bool AddUserToGroup(common.clsUser User, string strLDAPPath)
		{
			try
			{
								de		= new DirectoryEntry(strLDAPPath, strAdminUsr, strAdminPwd);
				DirectoryEntry	deUser	= this.GetUserByLoginID(User.LoginID);
				
				if ( !de.Properties["member"].Contains(deUser.Properties["distinguishedName"].Value) )
				{
					de.Properties["member"].Add( deUser.Properties["distinguishedName"].Value );
					de.CommitChanges();
					de.RefreshCache();
				}
				
				return de.Properties["member"].Contains(deUser.Properties["distinguishedName"].Value);
			}
			catch (System.Exception ex)
			{
				clsErr = new common.LogError();
				clsErr.WriteLogEntry(common.LogError.LogEntryType.Error, "PBSConnectBusinessActiveDirectoryManager", "Unable to AddUserToGroup! " + strLDAPPath + " ", ex);
				
				return false;
			}
		}
		#endregion
		
		#region public bool RemoveUserFromGroup(common.clsUser User, string strLDAPPath)
		public bool RemoveUserFromGroup(common.clsUser User, string strLDAPPath)
		{
			try
			{
								de		= new DirectoryEntry(strLDAPPath, strAdminUsr, strAdminPwd);
				DirectoryEntry	deUser	= this.GetUserByLoginID(User.LoginID);
				
				if ( de.Properties["member"].Contains(deUser.Properties["distinguishedName"].Value) )
				{
					de.Properties["member"].Remove( deUser.Properties["distinguishedName"].Value );
					de.CommitChanges();
					de.RefreshCache();
				}
				
				return !de.Properties["member"].Contains(deUser.Properties["distinguishedName"].Value);
			}
			catch (System.Exception ex)
			{
				clsErr = new common.LogError();
				clsErr.WriteLogEntry(common.LogError.LogEntryType.Error, "PBSConnectBusinessActiveDirectoryManager", "Unable to RemoveUserFromGroup! " + strLDAPPath + " ", ex);
				
				return false;
			}
		}
		#endregion
		
		#region  public bool AddToOU(common.clsUser User, string strLDAPPath)
		public bool AddToOU(common.clsUser User, string strLDAPPath)
		{
			this.strLDAPPath = strLDAPPath;
			
			try
			{
				de = new DirectoryEntry(this.strLDAPPath, strAdminUsr, strAdminPwd);
			
				// Check for existence of User
				DirectoryEntry	deUser		= this.GetUser("CN", User.DisplayName);
			
				// Note: Active Directory will reject attempts to add dupe CNs, so 
				// we have to check DiplayName instead of LoginID
				//		DirectoryEntry		deUser		= this.GetUserByLoginID(User.LoginID);
				//		if (!deUser.Properties.Contains("samAccountName"))
			
				// No User found, Add to AD tree
				if (deUser.Properties["samAccountName"].Value == null)
				{
					deUser =	de.Children.Add("CN=" + User.DisplayName, "user");
				}
				
				// Set the samAccountName, then commit changes to the directory entry and it's parent
				deUser.UsePropertyCache							= true;
				deUser.Properties["samAccountName"].Value		= User.LoginID;
				deUser.Properties["userPrincipalName"].Value	= User.LoginID + "@" + ConfigurationSettings.AppSettings["ActiveDirectoryRootString"].Replace("LDAP://","");
				deUser.CommitChanges();
			
				// Get the new AD path to this User
				this.strLDAPPath = this.GetUser("CN", User.DisplayName).Path;
			
				return true;
			}
			catch (System.Exception ex)
			{
				clsErr = new common.LogError();
				clsErr.WriteLogEntry(common.LogError.LogEntryType.Error, "PBSConnectBusinessActiveDirectoryManager", "Unable to AddToOU! " + strLDAPPath + " ", ex);
				
				return false;
			}
		}
		#endregion
		
		#region  public bool RemoveFromOU(common.clsUser User, string strLDAPPath)	
		public bool RemoveFromOU(common.clsUser User, string strLDAPPath)
		{
			try
			{
				de = new DirectoryEntry(strLDAPPath, strAdminUsr, strAdminPwd);
				de.Children.Remove(this.GetUserByLoginID(User.LoginID));
				
				// Get LastIndexOf(User.LoginID)
				int ixLogin = this.GetUsers( User.Profile.Organization ).ToString().LastIndexOf(User.LoginID);
				
				// Return success/failure
				return ixLogin < 0 ? true : false;
			}
			catch (System.Exception ex)
			{
				clsErr = new common.LogError();
				clsErr.WriteLogEntry(common.LogError.LogEntryType.Error, "PBSConnectBusinessActiveDirectoryManager", "Unable to RemoveFromOU! " + strLDAPPath + " ", ex);
				
				return false;
			}
		}
		#endregion
		
		
		
		#region public string[] GetGroups(common.clsUser User, string strLDAPPath)
		public string[] GetGroups(common.clsUser User, string strLDAPPath)
		{
			string strGroups = this.GetGroupsRecursive(User, strLDAPPath);
			if (strGroups.Length > 0)
			{
				return strGroups.Split(new char[]{'|'});
			}
			else
			{
				return new string[0];
			}
		}
		#endregion
		
		#region private string GetGroupsRecursive(common.clsUser User, string strLDAPPath)
		private string GetGroupsRecursive(common.clsUser User, string strLDAPPath)
		{
			// Get the AD DirectoryEntry per strLDAPPath
			DirectoryEntry	deToCheck		= ((strLDAPPath == null) == true) ? this.GetUserByLoginID(User.LoginID) : new DirectoryEntry(strLDAPPath, CONFIG_ADSI_AdminUsr, CONFIG_ADSI_AdminPwd);
			StringBuilder	sb				= new StringBuilder("");
			DirectoryEntry	deTest;
			string			cn,strGroupName;
			int				ixEquals,ixComma;
			
			for (int i=0; i < deToCheck.Properties["memberOf"].Count; i++)
			{
				cn				= (string) deToCheck.Properties["memberOf"][i];
				deTest			= new DirectoryEntry(CONFIG_ADSI_Root+"/"+cn, CONFIG_ADSI_AdminUsr, CONFIG_ADSI_AdminPwd);
				
				ixEquals		= cn.IndexOf("=", 1);
				ixComma			= cn.IndexOf(",", 1);
				strGroupName	= cn.Substring( (ixEquals + 1), (ixComma-ixEquals)-1 );
				
				if (sb.ToString().IndexOf(strGroupName) < 0 || sb.ToString().IndexOf(strGroupName+"|") < 0)
				{
					sb.Append(	strGroupName + "|"	);
				}
				
				if (deTest.Properties.Contains("memberOf"))
				{
					sb.Append( this.GetGroupsRecursive( User, CONFIG_ADSI_Root+"/"+cn )+"|" );
				}
			}
			deToCheck.Close();
			deToCheck.Dispose();
			
			string	strGroups = sb.ToString();
			if (strGroups.EndsWith("|"))
			{
				// Remove trailing pipe char
				strGroups = strGroups.Remove(strGroups.Length-1, 1);
			}
			
			return strGroups;
		}
		#endregion
		
		#region public string[] GetUsers(string strMemberStationOU)
		public string[] GetUsers(string strMemberStationOU)
		{
			try
			{
				// Get the DirectoryEntry for the supplied MemberStation OU
				strLDAPPath	+= "/OU=" + strMemberStationOU + ",OU=MemberStations," + strLDAPLocalDomain;
				de			 = new DirectoryEntry(strLDAPPath);
				
				IEnumerator				IEnumUsers	= de.Children.GetEnumerator();
				StringBuilder			sb			= new StringBuilder("");
				while (IEnumUsers.MoveNext())
				{
					// Get the DirectoryEntry for this User object
					DirectoryEntry	deUser			= (DirectoryEntry) IEnumUsers.Current;
					
					// GetGroups and check that User is a memberOf MemberStationOUs Admin or User group
					IEnumerator		IEnumGroups		= this.GetGroups(new common.clsUser(), deUser.Path).GetEnumerator();
					while (IEnumGroups.MoveNext())
					{
						if (	IEnumGroups.Current.ToString() == strMemberStationOU + "Admin"
							||	IEnumGroups.Current.ToString() == strMemberStationOU + "User")
						{
							string	strSAMAccount	= deUser.Properties["sAMAccountName"].Value.ToString();
							string	strDisplayName	= deUser.Properties["displayName"].Value.ToString();
							string	strLastUpdated	= deUser.Properties["whenChanged"].Value.ToString();
							
							// Append StringBuilder with the User entry and pipe delimiter
							// Individual fields per User are delimited with "+" char
							// Each row is then split by the caller
							sb.Append(		strSAMAccount	+ "+" 
										+	strDisplayName	+ "+"
										+	strLastUpdated	+ 
									"|");
						}
					}
					deUser.Dispose();
				}
				de.Dispose();
				
				// Return string[]
				string	strUsers = sb.ToString();
				if (strUsers.Length > 0)
				{
					// Removing trailing pipe char
					strUsers = strUsers.Remove(strUsers.Length-1, 1);
					return strUsers.Split(new char[]{'|'});
				}
				else
				{
					return new string[0];
				}
			}
			catch (System.Exception ex)
			{
				throw new System.Exception( "Path: " + strLDAPPath + " Message: " + ex.Message ); 
			}
		}
		#endregion
		
		#region public DirectoryEntry GetUserByLoginID(string strLoginID)
		public DirectoryEntry GetUserByLoginID(string strLoginID)
		{
			return this.GetUser("samAccountName", strLoginID);
		}
		#endregion
		
		#region public DirectoryEntry GetUserByEmailAddress(string strEmailAddress)
		public DirectoryEntry GetUserByEmailAddress(string strEmailAddress)
		{
			return this.GetUser("mail", strEmailAddress);
		}
		#endregion
		
		#region private DirectoryEntry GetUser(string strADPropertyKey, string strADPropertyValue)
		private DirectoryEntry GetUser(string strADPropertyKey, string strADPropertyValue)
		{
			DirectorySearcher	srch = new DirectorySearcher(strLDAPPath);
			
			// Match on key/value pair supplied
			srch.Filter = "(" + strADPropertyKey + "=" + strADPropertyValue + ")";
			
			srch.PropertiesToLoad.Add("CN");
			SearchResult		srchResult = srch.FindOne();
			
			// User is valid in Active Directory
			if (srchResult != null)
			{
				try		{ return new DirectoryEntry(srchResult.Path, strAdminUsr, strAdminPwd); }
				catch	{ return new DirectoryEntry(); }
			}
			else
			{
				return new DirectoryEntry();
			}
		}
		#endregion
		
		#region public DirectoryEntry GetUser(string strLDAPPathToUser)
		public DirectoryEntry GetUser(string strLDAPPathToUser)
		{
			return new DirectoryEntry(strLDAPPathToUser, strAdminUsr, strAdminPwd);
		}
		#endregion
		
		
		
		#region public DataSet GetADDataSet(string strLDAPPathToSearch, string strToFilterSearchBy)
		public DataSet GetADDataSet(string strLDAPPathToSearch, string strToFilterSearchBy)
		{
			DataSet	dsResults = new DataSet("ADSI_SEARCH_RESULTS");
			// Add Table and Columns to DataSet
			dsResults.Tables.Add();
			dsResults.Tables[0].Columns.Add("GUID");
			dsResults.Tables[0].Columns.Add("Path");
			dsResults.Tables[0].Columns.Add("Name");
			dsResults.Tables[0].Columns.Add("Properties");
			try
			{
				DirectoryEntry		deSearch		= new DirectoryEntry(strLDAPPathToSearch, this.strAdminUsr, this.strAdminPwd);
				DirectorySearcher	objSearch		= new DirectorySearcher(deSearch, strToFilterSearchBy);
				objSearch.Sort.PropertyName	= "Name";
				IEnumerator			IEnumResults	= objSearch.FindAll().GetEnumerator();
				
				// Loop SearchResults
				while (IEnumResults.MoveNext())
				{
					DirectoryEntry	deChild		= ((SearchResult) IEnumResults.Current).GetDirectoryEntry();
					object[]		arrChild	= new object[4];
				
					// Set DataSet column values
					arrChild.SetValue(deChild.Guid,										0);
					arrChild.SetValue(deChild.Path,										1);
					arrChild.SetValue(deChild.Name.Replace("CN=","").Replace("OU=",""),	2);
					arrChild.SetValue(deChild.Properties,								3);
				
					// Add row to DataSet
					dsResults.Tables[0].Rows.Add( arrChild );
				}
				
				// Clean up
				objSearch.Dispose();
				deSearch.Close();
				deSearch.Dispose();
			}
			catch { }
			
			// Return DataSet
			return dsResults;
		}
		#endregion
		
		#endregion
		
		#region clsPBSActiveDirectory properties
		
		public static string	CONFIG_ADSI_Root;
		public static string	CONFIG_ADSI_AdminUsr;
		public static string	CONFIG_ADSI_AdminPwd;
		public static string	CONFIG_ADSI_DistinguishedName;
		
		#endregion
	}
	#endregion
	
	#region public class clsActiveDirectoryManager
	/// <summary>This class provides methods for querying, creating, updating, and deleting Active Directory objects</summary>
	public class clsActiveDirectoryManager
	{
		clsPBSActiveDirectory _clsPBSActiveDirectory;
		
		#region clsActiveDirectoryManager constructors
		/// <summary>This constructor accepts 2 params</summary>
		/// <param name="strAdminUsr">The domain user with full permission to on all Active Directory objects</param>
		/// <param name="strAdminPwd">The admin user's password</param>
		public clsActiveDirectoryManager(string strAdminUsr, string strAdminPwd)
		{
			clsPBSActiveDirectory.CONFIG_ADSI_Root					= ConfigurationSettings.AppSettings["ActiveDirectoryRootString"];
			clsPBSActiveDirectory.CONFIG_ADSI_DistinguishedName	= null;
			clsPBSActiveDirectory.CONFIG_ADSI_AdminUsr				= strAdminUsr;
			clsPBSActiveDirectory.CONFIG_ADSI_AdminPwd				= strAdminPwd;
			
			_clsPBSActiveDirectory = new clsPBSActiveDirectory();
		}
		/// <summary>This constructor accepts 3 params</summary>
		/// <param name="strAdminUsr">The domain user with full permission to on all Active Directory objects</param>
		/// <param name="strAdminPwd">The admin user's password</param>
		/// <param name="strDistinguishedName">The full LDAP path to a domain user object</param>
		public clsActiveDirectoryManager(string strAdminUsr, string strAdminPwd, string strDistinguishedName)
		{
			clsPBSActiveDirectory.CONFIG_ADSI_Root					= ConfigurationSettings.AppSettings["ActiveDirectoryRootString"];
			clsPBSActiveDirectory.CONFIG_ADSI_DistinguishedName	= strDistinguishedName;
			clsPBSActiveDirectory.CONFIG_ADSI_AdminUsr				= strAdminUsr;
			clsPBSActiveDirectory.CONFIG_ADSI_AdminPwd				= strAdminPwd;
			
			_clsPBSActiveDirectory = new clsPBSActiveDirectory();
		}
		
		#endregion
		
		#region clsActiveDirectoryManager methods
		
		#region public bool CreateADAccount(common.clsUser User, common.clsPassword Password)
		/// <summary>This method creates a new account. The following order of operations occurs: 
		/// 1) Add User to OU=[MemberStation]
		/// 2) Set User account properties (Profile)
		/// 3) Set User account password
		/// 4) Set User account = Enabled
		/// 5) Add User to [MemberStation]Usr Group
		/// 6) Add User to "PBSC All Users" Group
		/// </summary>
		/// <param name="User">The Common.User to create</param>
		/// <param name="Password">The Common.Password of the User</param>
		/// <returns>bool</returns>
		public bool CreateADAccount(common.clsUser User, common.clsPassword Password)
		{
			return _clsPBSActiveDirectory.CreateADAccount(User, Password);
		}
		#endregion
		
		#region public bool UpdateADAccount(common.clsUser User, common.clsPassword Password)
		/// <summary>This method updates an account</summary>
		/// <param name="User">The User object to update</param>
		/// <param name="Password">Not used</param>
		/// <returns>bool</returns>
		public bool UpdateADAccount(common.clsUser User, common.clsPassword Password)
		{
			return _clsPBSActiveDirectory.UpdateADAccount(User, Password);
		}
		#endregion
		
		#region public bool DeleteADAccount(common.clsUser User, common.clsPassword Password)
		/// <summary>
		/// This method deletes an account
		/// </summary>
		/// <param name="User">The User to delete</param>
		/// <param name="Password">Not used</param>
		/// <returns>bool</returns>
		public bool DeleteADAccount(common.clsUser User, common.clsPassword Password)
		{
			return _clsPBSActiveDirectory.DeleteADAccount(User, Password);
		}
		#endregion
		
		#region public bool ResetPassword(common.User clsUser, common.Password clsPwd)
		/// <summary>This method resets a User's password. Calls native AD "setPassword" method</summary>
		/// <param name="User">The User to be reset</param>
		/// <param name="Password">The Password object containing NewPassword</param>
		/// <returns>bool</returns>
		public bool ResetPassword(common.clsUser User, common.clsPassword Password)
		{
			return _clsPBSActiveDirectory.ResetPassword(User, Password);
		}
		#endregion
		
		#region public bool EnableADAccount(common.clsUser User)
		/// <summary>This method enables a User account. This method is flagged as an unsafe code block (see MSDN docs on the ADS_USER_FLAGS)</summary>
		/// <param name="User">The User to enable</param>
		/// <returns>bool</returns>
		public bool EnableADAccount(common.clsUser User)
		{
			return _clsPBSActiveDirectory.EnableADAccount(User);
		}
		#endregion
		
		#region public bool DisableADAccount(common.clsUser User)
		/// <summary>This method disables a User account. This method is flagged as an unsafe code block (see MSDN docs on the ADS_USER_FLAGS)</summary>
		/// <param name="User">The User to disable</param>
		/// <returns>bool</returns>
		public bool DisableADAccount(common.clsUser User)
		{
			return _clsPBSActiveDirectory.DisableADAccount(User);
		}
		#endregion
		
		#region public bool IsADAccountEnabled(common.clsUser User)
		/// <summary>This method checks to see if a User account is enabled. This method is flagged as an unsafe code block (see MSDN docs on the ADS_USER_FLAGS)</summary>
		/// <param name="User">The User whose account needs to be checked</param>
		/// <returns>bool</returns>
		public bool IsADAccountEnabled(common.clsUser User)
		{
			return _clsPBSActiveDirectory.IsADAccountEnabled(User);
		}
		#endregion
		
		
		
		#region public bool AddUserToGroup(common.clsUser User, string strLDAPPath)
		/// <summary>This method adds a User to an AD group</summary>
		/// <param name="User">The User to add</param>
		/// <param name="strLDAPPath">The full LDAP path to the Group object</param>
		/// <returns>bool</returns>
		public bool AddUserToGroup(common.clsUser User, string strLDAPPath)
		{
			return _clsPBSActiveDirectory.AddUserToGroup(User, strLDAPPath);
		}
		#endregion
		
		#region public bool RemoveUserFromGroup(common.clsUser User, string strLDAPPath)
		/// <summary>This method removes a User from an AD group</summary>
		/// <param name="User">The User to remove</param>
		/// <param name="strLDAPPath">The full LDAP path to the Group object</param>
		/// <returns>bool</returns>
		public bool RemoveUserFromGroup(common.clsUser User, string strLDAPPath)
		{
			return _clsPBSActiveDirectory.RemoveUserFromGroup(User, strLDAPPath);
		}
		#endregion

		#region public bool AddToOU(common.clsUser User, string strLDAPPath)
		/// <summary>This method adds a User to the OU specified</summary>
		/// <param name="User">The User to add</param>
		/// <param name="strLDAPPath">The full LDAP path to the OU object</param>
		/// <returns>bool</returns>
		public bool AddToOU(common.clsUser User, string strLDAPPath)
		{
			return _clsPBSActiveDirectory.AddToOU(User, strLDAPPath);
		}
		#endregion
		
		#region public bool RemoveFromOU(common.clsUser User, string strLDAPPath)
		/// <summary>This method removes a User from the OU specified</summary>
		/// <param name="User">The User to remove</param>
		/// <param name="strLDAPPath">The full LDAP path to the OU object</param>
		/// <returns>bool</returns>
		public bool RemoveFromOU(common.clsUser User, string strLDAPPath)
		{
			return _clsPBSActiveDirectory.RemoveFromOU(User, strLDAPPath);
		}
		#endregion
		
		
		
		#region public string[] GetGroups(common.clsUser User, string strLDAPPath)
		/// <summary>This method retrieves the User's group memberships</summary>
		/// <param name="User">The User object whose "memberOf" property needs to be accessed</param>
		/// <param name="strLDAPPath">The full LDAP path to the User</param>
		/// <returns>string[]</returns>
		public string[] GetGroups(common.clsUser User, string strLDAPPath)
		{
			try
			{
				return _clsPBSActiveDirectory.GetGroups(User, strLDAPPath);
			}
			catch (System.Exception ex)
			{
				throw new System.Exception( ex.GetBaseException().Message );
			}
		}
		#endregion
		
		#region public string[] GetUsers(string strCallLetters)
		/// <summary>This method retrieves a list of Users from the specified MemberStation\[StationCallLetters] OU</summary>
		/// <param name="strCallLetters">The StationCallLetters OU</param>
		/// <returns>string[] in the following format: [strSAMAccount]+"+"+[strDisplayName]+"+"+[strLastUpdated]</returns>
		public string[] GetUsers(string strCallLetters)
		{
			try
			{
				return _clsPBSActiveDirectory.GetUsers(strCallLetters);
			}
			catch (System.Exception ex)
			{
				throw new System.Exception( ex.GetBaseException().Message );
			}
		}
		#endregion
		
		#region public DataSet GetUsers(string strCallLetters)
		/*
		public DataSet GetUsers(string strCallLetters)
		{
						clsADMngr	= new ad.clsActiveDirectoryManager();
			string[]	arrUsers	= this.GetUsers(strCallLetters);
			IEnumerator	IEnumUsers	= arrUsers.GetEnumerator();
			DataSet		ds			= new DataSet("Users");
									ds.Tables.Add();
									ds.Tables[0].Columns.Add("LoginID");
									ds.Tables[0].Columns.Add("DisplayName");
									ds.Tables[0].Columns.Add("LastUpdated");
			while (IEnumUsers.MoveNext())
			{
				// Split the array member on + char and cast as object[] to fill DataSet columns
				object[] arrUser = (object[]) IEnumUsers.Current.ToString().Split(new char[]{'+'});
				
				// Add row to DataSet
				ds.Tables[0].Rows.Add( arrUser );
			}
			return ds;
		}
		*/
		#endregion
		
		#region public DirectoryEntry GetUser(string strLDAPPathToUser)
		/// <summary>This method retrieves an AD User object</summary>
		/// <param name="strLDAPPathToUser">The full LDAP path to the User object</param>
		/// <returns>DirectoryEntry</returns>
		public DirectoryEntry GetUser(string strLDAPPathToUser)
		{
			return _clsPBSActiveDirectory.GetUser(strLDAPPathToUser);
		}
		#endregion

		#region public DirectoryEntry GetUserByLoginID(string strLoginID)
		/// <summary>This method retrieves an AD User object, first searching on samAccountName</summary>
		/// <param name="strLoginID">The LoginID (samAccountName) of the User</param>
		/// <returns>DirectoryEntry</returns>
		public DirectoryEntry GetUserByLoginID(string strLoginID)
		{
			return _clsPBSActiveDirectory.GetUserByLoginID(strLoginID);
		}
		#endregion
		
		#region public DirectoryEntry GetUserByEmailAddress(string strEmailAddress)
		/// <summary>This method retrieves an AD User object, first searching on mail</summary>
		/// <param name="strEmailAddress">The Email Address (mail) of the User</param>
		/// <returns>DirectoryEntry</returns>
		public DirectoryEntry GetUserByEmailAddress(string strEmailAddress)
		{
			return _clsPBSActiveDirectory.GetUserByEmailAddress(strEmailAddress);
		}
		#endregion
		
		#region public DataSet GetADDataSet(string strLDAPPathToSearch, string strToFilterSearchBy)
		/// <summary>This method creates a DataSet based on query filter supplied</summary>
		/// <param name="strLDAPPathToSearch">The LDAP path to search in</param>
		/// <param name="strToFilterSearchBy">The query used to filter the objects (i.e. "(CN=*)", "(OU=*)"), etc. For more help on creating effective query filters, see http://msdn.microsoft.com/library/default.asp?url=/library/en-us/ad/ad/creating_a_query_filter.asp</param>
		/// <returns>System.Data.DataSet</returns>
		public DataSet GetADDataSet(string strLDAPPathToSearch, string strToFilterSearchBy)
		{
			return _clsPBSActiveDirectory.GetADDataSet(strLDAPPathToSearch, strToFilterSearchBy);
		}
		#endregion

		#region public string GetRawUserProperties(string strLoginID)
		/// <summary>This method is used for debug to print the raw Active Directory Properties of a User</summary>
		/// <param name="strLoginID">The LoginID (samAccountName) of the User whose properties need to be returned</param>
		/// <returns>string</returns>	
		public string GetRawUserProperties(string strLoginID)
		{
			try
			{
				StringBuilder									sb			= new StringBuilder("");
				System.DirectoryServices.PropertyCollection		pc			= this.GetUserByLoginID(strLoginID).Properties;
				IDictionaryEnumerator							IEnumProps	= pc.GetEnumerator();
				
				// Loop the Properties collection
				while (IEnumProps.MoveNext())
				{
					// Print the Key
					sb.Append(IEnumProps.Key.ToString());
				
					// Loop the Values collection
					PropertyValueCollection	pvc			= (System.DirectoryServices.PropertyValueCollection) IEnumProps.Value;
					IEnumerator				IEnumValues	= pvc.GetEnumerator();
					while (IEnumValues.MoveNext())
					{
						// Print the Value
						sb.Append("<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + IEnumValues.Current.ToString());
					}
					sb.Append("<br>");
				}
					
				// Append strMessage with Property output
				return sb.ToString();
			}
			catch
			{
				return "Error retrieving Login Properties";
			}
		}
		#endregion
		
		#endregion
	}
	#endregion
}
