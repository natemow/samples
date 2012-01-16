using System;

namespace OSA.WIO.Applications.Objects
{
	public class clsUser
	{
		int				intUserID,intSiteID,intAcctID,intStateID,intCountryID,intLocalSectionID;
		int				intPageIDToProfile;
		string			strPhone,strPhoneFax;
		string			strStateName,strCountryName;
		string			strLoginID,strLoginPwd,strMemberID,strNameFirst,strNameMiddle,strNameLast,strNameDisplay,strAdd1,strAdd2,strAddDept,strCity,strZip,strEmailPrivate;
		enumRole		enumRole;
		clsCompany		clsComp;
		bool			boolIsPrimary;
		
		#region User properties
		
		public	int				UserID				{ get{return intUserID;}			set{intUserID = value;} }
		public	int				SiteID				{ get{return intSiteID;}			set{intSiteID = value;} }
		public	int				AcctID				{ get{return intAcctID;}			set{intAcctID = value;} }
		public	enumRole		Role				{ get{return enumRole;}				set{enumRole = value;} }
		public	string			RoleDisplay			{ get{return Role.ToString().Replace("Job", "Job ");} }
		public	string			LoginID				{ get{return strLoginID;}			set{strLoginID = value;} }
		public	string			LoginPassword		{ get{return strLoginPwd;}			set{strLoginPwd = value;} }
		public	string			MemberID			{ get{return strMemberID;}			set{strMemberID = value;} }
		public	int				LocalSectionID		{ get{return intLocalSectionID;}	set{intLocalSectionID = value;} }
		public	string			NameFirst			{ get{return strNameFirst;}			set{strNameFirst = value;} }
		public	string			NameMiddle			{ get{return strNameMiddle;}		set{strNameMiddle = value;} }
		public	string			NameLast			{ get{return strNameLast;}			set{strNameLast = value;} }
		public	string			NameDisplay			{ get{return strNameDisplay;}		set{strNameDisplay = value;} }
		public	string			Address1			{ get{return strAdd1;}				set{strAdd1 = value;} }
		public	string			Address2			{ get{return strAdd2;}				set{strAdd2 = value;} }
		public	string			AddressDept			{ get{return strAddDept;}			set{strAddDept = value;} }
		public	string			AddressCity			{ get{return strCity;}				set{strCity = value;} }
		public	string			AddressZip			{ get{return strZip;}				set{strZip = value;} }
		public	int				AddressStateID		{ get{return intStateID;}			set{intStateID = value;} }
		public	string			AddressStateName	{ get{return strStateName;}			set{strStateName = value;} }
		public	int				AddressCountryID	{ get{return intCountryID;}			set{intCountryID = value;} }
		public	string			AddressCountryName	{ get{return strCountryName;}		set{strCountryName = value;} }
		public	string			EmailPrivate		{ get{return strEmailPrivate;}		set{strEmailPrivate = value;} }
		public	string			Phone				{ get{return strPhone;}				set{strPhone = value;} }
		public	string			PhoneFax			{ get{return strPhoneFax;}			set{strPhoneFax = value;} }
		public	int				PageIDToProfile		{ get{return intPageIDToProfile;}	set{intPageIDToProfile = value;} }
		
		public	clsCompany		Company				{ get{return clsComp;}				set{clsComp = value;} }
		public	bool			IsAcctPrimaryUser	{ get{return boolIsPrimary;}		set{boolIsPrimary = value;} }
		
		#endregion
	}
}
