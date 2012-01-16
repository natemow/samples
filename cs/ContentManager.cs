using System;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Text;

using	common	= natemow.Applications.Objects;
using	data	= natemow.Data.Access;

namespace natemow.Applications.ContentManager
{
	public class clsContentManager
	{
		string			strConn,strMessage;
		bool			boolResult;
		object[]		spParams;
		SqlDataReader	dr;
		
		int				intPageID;
		string			strWebHostUrl;
		common.clsWeb	clsWeb;
		common.clsPage	clsPage;
		
		#region clsContentManager constructor
		public clsContentManager(string strDBConn, string strHttpHost, int intPageID)
		{
			this.strConn		= strDBConn;
			this.strWebHostUrl	= strHttpHost;
			this.intPageID		= intPageID;
			this.strMessage		= "";
			
			spParams = new object[3];
			spParams.SetValue(strWebHostUrl,	0);
			spParams.SetValue(null,				1);
			spParams.SetValue(intPageID,		2);
			
			clsWeb = this.GetWeb( data.SqlHelper.ExecuteReader(strConn, "prc_WEB_PAGE_sel", spParams) );
		}
		#endregion
		
		#region clsContentManager methods
		
		#region private void GetSqlTransResult(SqlDataReader dr)
		private void GetSqlTransResult(SqlDataReader dr)
		{
			dr.Read();
			boolResult	= bool.Parse(dr["Result"].ToString());
			strMessage	= "\r\t\t" + dr["MessageOut"].ToString() + "<br>";
			dr.Close();
		}
		#endregion
		
		#region private string GetSqlSplitString(string[] arrSplit)
		private string GetSqlSplitString(string[] arrSplit)
		{
			// Create a pipe-delimited string to be parsed via SQL fn_split function
			IEnumerator		IEnumSplit	= arrSplit.GetEnumerator();
			StringBuilder	sbSplit		= new StringBuilder("");
			string			strSplit;
			while (IEnumSplit.MoveNext()) { sbSplit.Append( IEnumSplit.Current.ToString().Trim() + "|" ); }
			strSplit	= sbSplit.ToString();
			if (strSplit.Length > 0)
			{
				strSplit = strSplit.Remove(strSplit.Length-1, 1);
			}
			
			return strSplit;
		}
		#endregion
		
		
		
		#region private string[] GetDomains(common.clsWeb Web)
		private string[] GetDomains(common.clsWeb Web)
		{
			spParams = new object[1];
			spParams.SetValue(Web.WebID,	0);
			
			SqlDataReader	drDomains	= data.SqlHelper.ExecuteReader(this.strConn, "prc_WEB_HOST_sel", spParams);
			StringBuilder	sbDomains		= new StringBuilder("");
			string			strDomains;
			string[]		arrDomains;
			
			while(drDomains.Read()) { sbDomains.Append(drDomains["web_host_url"].ToString() + "|"); }
			drDomains.Close();
			
			strDomains = sbDomains.ToString();
			if (strDomains.Length > 0)
			{
				strDomains = strDomains.Remove(strDomains.Length-1, 1);
				arrDomains = strDomains.Split(new char[]{'|'});
				return arrDomains;
			}
			
			return new string[0];
		}
		#endregion
		
		#region private string[] GetRoles(SqlDataReader drOfRoles)
		private string[] GetRoles(SqlDataReader drOfRoles)
		{
			StringBuilder	sbRoles		= new StringBuilder("");
			string			strRoles;
			string[]		arrRoles;
			
			while(drOfRoles.Read()) { sbRoles.Append(drOfRoles["role_name"].ToString().Trim() + "|"); }
			drOfRoles.Close();
			
			strRoles = sbRoles.ToString();
			if (strRoles.Length > 0)
			{
				strRoles = strRoles.Remove(strRoles.Length-1, 1);
				arrRoles = strRoles.Split(new char[]{'|'});
				return arrRoles;
			}
			
			return new string[0];
		}
		#endregion
		
		#region public string[] GetRoles(common.clsWeb Web)
		public string[] GetRoles(common.clsWeb Web)
		{
			spParams = new object[1];
			spParams.SetValue(Web.WebID, 0);
			
			return this.GetRoles( data.SqlHelper.ExecuteReader(this.strConn, "prc_ROLE_WEB_sel", spParams) );
		}
		#endregion
		
		#region private string[] GetRoles(common.clsPage Page)
		private string[] GetRoles(common.clsPage Page)
		{
			spParams = new object[1];
			spParams.SetValue(Page.PageID,	0);
			
			return this.GetRoles( data.SqlHelper.ExecuteReader(this.strConn, "prc_ROLE_WEB_PAGE_sel", spParams) );
		}
		#endregion
		
		
		
		#region public DataSet GetWebs(string strSortExpression)
		public DataSet GetWebs(string strSortExpression)
		{
			spParams = new object[1];
			spParams.SetValue(strSortExpression,	0);
			
			return data.SqlHelper.ExecuteDataset(this.strConn, "prc_WEB_sel", spParams);
		}
		#endregion
		
		#region private common.clsWeb GetWeb(SqlDataReader drToRead)
		private common.clsWeb GetWeb(SqlDataReader drToRead)
		{
			// Set the Web and Page objects
			drToRead.Read();
			
			clsWeb	= new common.clsWeb();
			clsWeb.WebID			= int.Parse(drToRead["web_id"].ToString());
			clsWeb.WebIDParent		= int.Parse(drToRead["web_id_parent"].ToString());
			clsWeb.WebGUID			= drToRead["web_guid"].ToString();
			clsWeb.Title			= drToRead["web_title"].ToString();
			clsWeb.MailFromAddress	= drToRead["web_mail_from"].ToString();
			clsWeb.PageFooter		= drToRead["page_footer"].ToString();
			clsWeb.PageIDHome		= int.Parse(drToRead["page_id_home"].ToString());
			clsWeb.PathToStylesheet	= drToRead["path_to_stylesheet"].ToString();
			clsWeb.PathToDocs		= drToRead["path_to_docs"].ToString();
			clsWeb.PathToImages		= drToRead["path_to_images"].ToString();
			clsWeb.Status			= (common.Status) int.Parse(drToRead["web_status"].ToString());
			clsWeb.Roles			= this.GetRoles(clsWeb);
			clsWeb.Domains			= this.GetDomains(clsWeb);
			
			clsPage = new common.clsPage();
			clsPage.PageID			= int.Parse(drToRead["page_id"].ToString());
			clsPage.PageIDParent	= int.Parse(drToRead["page_id_parent"].ToString());
			clsPage.WebID			= clsWeb.WebID;
			clsPage.WizardID		= (drToRead["wizard_id"].ToString().Length > 0) ? int.Parse(drToRead["wizard_id"].ToString()) : 0;
			clsPage.SortOrder		= int.Parse(drToRead["sort_order"].ToString());
			clsPage.ShowOnMenu		= bool.Parse(drToRead["yn_showonmenu"].ToString());
			clsPage.IsNewsPage		= bool.Parse(drToRead["yn_news"].ToString());
			clsPage.IsNewsArchive	= bool.Parse(drToRead["yn_news_archive"].ToString());
			clsPage.Title			= drToRead["page_title"].ToString();
			clsPage.ControlID		= (drToRead["control_id"].ToString().Length > 0) ? int.Parse(drToRead["control_id"].ToString()) : 0;
			clsPage.Control			= (drToRead["page_control"].ToString().Length > 0) ? drToRead["page_control"].ToString() : null;
			clsPage.Image			= (drToRead["page_image"].ToString().Length > 0) ? drToRead["page_image"].ToString() : null;
			clsPage.Body			= (drToRead["page_body"].ToString().Length > 0) ? drToRead["page_body"].ToString() : null;
			clsPage.Roles			= this.GetRoles(clsPage);
			
			drToRead.Close();
			
			clsWeb.Page	= clsPage;
			
			return clsWeb;
		}
		#endregion
		
		#region public common.clsWeb GetWeb(System.Guid GUID, int intPageID)
		public common.clsWeb GetWeb(System.Guid GUID, int intPageID)
		{
			spParams = new object[3];
			spParams.SetValue(null,				0);
			spParams.SetValue(GUID.ToString(),	1);
			spParams.SetValue(intPageID,		2);
			
			return this.GetWeb( data.SqlHelper.ExecuteReader(this.strConn, "prc_WEB_PAGE_sel", spParams) );
		}
		#endregion
		
		#region public DataTable GetWebPageTree(int intPageID, char charDelimiter)
		public DataTable GetWebPageTree(int intPageID, char charDelimiter)
		{
			spParams = new object[2];
			spParams.SetValue(intPageID,		0);
			spParams.SetValue(charDelimiter,	1);
			
			DataSet ds = data.SqlHelper.ExecuteDataset(this.strConn, "prc_WEB_PAGE_TREE_sel", spParams);
			
			return ds.Tables[0];
		}
		#endregion
		
		#region public DataSet GetPageControls()
		public DataSet GetPageControls()
		{
			spParams = new object[0];
			return data.SqlHelper.ExecuteDataset(this.strConn, "prc_WEB_PAGE_CONTROL_sel", spParams);
		}
		#endregion
		
		#region public DataSet GetWebMenu(common.clsUser User)
		public DataSet GetWebMenu(common.clsUser User)
		{
			spParams = new object[2];
			spParams.SetValue(User.WebID,	0);
			spParams.SetValue(User.UserID,	1);
			
			return data.SqlHelper.ExecuteDataset(this.strConn, "prc_WEB_MENU_sel", spParams);
		}
		#endregion
		
		#region public DataSet GetWebWizard(string strWizardName, bool GetDetails)
		public DataSet GetWebWizard(string strWizardName, bool GetDetails)
		{
			spParams = new object[3];
			spParams.SetValue(0,				0);
			spParams.SetValue(strWizardName,	1);
			spParams.SetValue(GetDetails,		2);
			
			return data.SqlHelper.ExecuteDataset(this.strConn, "prc_WEB_WIZARD_sel", spParams);
		}
		#endregion
		
		#region public DataSet GetWebWizard(int intWizardID, bool GetDetails)
		public DataSet GetWebWizard(int intWizardID, bool GetDetails)
		{
			spParams = new object[3];
			spParams.SetValue(intWizardID,	0);
			spParams.SetValue(null,			1);
			spParams.SetValue(GetDetails,	2);
			
			return data.SqlHelper.ExecuteDataset(this.strConn, "prc_WEB_WIZARD_sel", spParams);
		}
		#endregion
		
		
		
		#region public common.clsWeb Web_Insert(common.clsWeb Web, common.clsUser User)
		public common.clsWeb Web_Insert(common.clsWeb Web, common.clsUser User)
		{
			string	strWebHosts = this.GetSqlSplitString(Web.Domains);
			string	strWebRoles	= this.GetSqlSplitString(Web.Roles);
			
			// Try to insert new Web
			spParams = new object[7];
			spParams.SetValue(0,					0);
			spParams.SetValue(User.UserID,			1);
			spParams.SetValue(Web.Title,			2);
			spParams.SetValue(Web.MailFromAddress,	3);
			spParams.SetValue(Web.PageFooter,		4);
			spParams.SetValue(strWebHosts,			5);
			spParams.SetValue(strWebRoles,			6);
			
			// Read data out to set the return Web object
			dr = data.SqlHelper.ExecuteReader(this.strConn, "prc_WEB_ins_upd", spParams);
			dr.Read();
			
			boolResult		= bool.Parse(dr["Result"].ToString());
			strMessage		= "\r\r\t" + dr["MessageOut"].ToString() + "<br>";
			
			Web.WebID		= int.Parse(dr["web_id"].ToString());
			Web.WebGUID		= dr["web_guid"].ToString();
			
			common.clsPage clsPage	= new common.clsPage();
			clsPage.PageID			= int.Parse(dr["page_id"].ToString());
			clsPage.PageIDParent	= clsPage.PageID;
			
			Web.Page = clsPage;
			
			dr.Close();
			
			return Web;
		}
		#endregion
		
		#region public common.clsWeb Web_Update(common.clsWeb Web, common.clsUser User)
		public common.clsWeb Web_Update(common.clsWeb Web, common.clsUser User)
		{
			string	strWebHosts = this.GetSqlSplitString(Web.Domains);
			string	strWebRoles	= this.GetSqlSplitString(Web.Roles);
			
			// Try to insert new Web
			spParams = new object[7];
			spParams.SetValue(Web.WebID,			0);
			spParams.SetValue(User.UserID,			1);
			spParams.SetValue(Web.Title,			2);
			spParams.SetValue(Web.MailFromAddress,	3);
			spParams.SetValue(Web.PageFooter,		4);
			spParams.SetValue(strWebHosts,			5);
			spParams.SetValue(strWebRoles,			6);
			
			// Read data out to set the return Web object
			dr = data.SqlHelper.ExecuteReader(this.strConn, "prc_WEB_ins_upd", spParams);
			dr.Read();
			
			boolResult		= bool.Parse(dr["Result"].ToString());
			strMessage		= "\r\r\t" + dr["MessageOut"].ToString() + "<br>";
			
			Web.WebID		= int.Parse(dr["web_id"].ToString());
			Web.WebGUID		= dr["web_guid"].ToString();
			
			common.clsPage clsPage	= new common.clsPage();
			clsPage.PageID			= int.Parse(dr["page_id"].ToString());
			clsPage.PageIDParent	= clsPage.PageID;
			
			Web.Page = clsPage;
			
			dr.Close();
			
			return Web;
		}
		#endregion
		
		#region public bool Web_Delete(common.clsWeb Web)
		public bool Web_Delete(common.clsWeb Web)
		{
			spParams = new object[1];
			spParams.SetValue(Web.WebID,			0);
			
			this.GetSqlTransResult( data.SqlHelper.ExecuteReader(strConn, "prc_WEB_del", spParams) );
			
			return boolResult;
		}
		#endregion
		
		#region private common.clsPage SetWebPage(common.clsPage Page, common.clsUser User)
		private common.clsPage SetWebPage(common.clsPage Page, common.clsUser User)
		{
			string strPageRoles = this.GetSqlSplitString(Page.Roles);
			
			// Try to insert/update WEB_PAGE
			common.clsPage clsPage = new common.clsPage();
			
			spParams = new object[13];
			spParams.SetValue(Page.PageID,			0);
			spParams.SetValue(Page.PageIDParent,	1);
			spParams.SetValue(Page.WebID,			2);
			spParams.SetValue(Page.SortOrder,		3);
			spParams.SetValue(Page.ShowOnMenu,		4);
			spParams.SetValue(Page.IsNewsPage,		5);
			spParams.SetValue(Page.IsNewsArchive,	6);
			spParams.SetValue(Page.Title,			7);
			spParams.SetValue(Page.Image,			8);
			spParams.SetValue(Page.ControlID,		9);
			spParams.SetValue(Page.Body,			10);
			spParams.SetValue(strPageRoles,			11);
			spParams.SetValue(User.UserID,			12);
			
			// Read data out to populate the return Page object
			dr = data.SqlHelper.ExecuteReader(this.strConn, "prc_WEB_PAGE_ins_upd", spParams);
			dr.Read();
			
			boolResult	= bool.Parse(dr["Result"].ToString());
			strMessage	= "\r\t\t" + dr["MessageOut"].ToString() + "<br>";
			Page.PageID	= int.Parse(dr["page_id"].ToString());
			
			dr.Close();
			
			clsPage.PageID			= Page.PageID;
			clsPage.PageIDParent	= Page.PageIDParent;
			clsPage.WebID			= Page.WebID;
			clsPage.SortOrder		= Page.SortOrder;
			clsPage.ShowOnMenu		= Page.ShowOnMenu;
			clsPage.IsNewsPage		= Page.IsNewsPage;
			clsPage.IsNewsArchive	= Page.IsNewsArchive;
			clsPage.Title			= Page.Title;
			clsPage.Image			= Page.Image;
			clsPage.ControlID		= Page.ControlID;
			clsPage.Body			= Page.Body;
			
			return clsPage;
		}
		#endregion
		
		#region public common.clsPage WebPage_Insert(common.clsPage Page, common.clsUser User)
		public common.clsPage WebPage_Insert(common.clsPage Page, common.clsUser User)
		{
			return this.SetWebPage(Page, User);
		}
		#endregion
		
		#region public common.clsPage WebPage_Update(common.clsPage Page, common.clsUser User)
		public common.clsPage WebPage_Update(common.clsPage Page, common.clsUser User)
		{
			return this.SetWebPage(Page, User);
		}
		#endregion
		
		#region public bool WebPage_Delete(common.clsPage Page)
		public bool WebPage_Delete(common.clsPage Page)
		{
			spParams = new object[1];
			spParams.SetValue(Page.PageID,	0);
			
			this.GetSqlTransResult( data.SqlHelper.ExecuteReader(strConn, "prc_WEB_PAGE_del", spParams) );
			
			return boolResult;
		}
		#endregion
		
		
		#region public DataSet WebPage_News_Select(bool IsArchive)
		public DataSet WebPage_News_Select(bool IsArchive)
		{
			spParams = new object[2];
			spParams.SetValue(strWebHostUrl,	0);
			spParams.SetValue(IsArchive,		1);
			
			return data.SqlHelper.ExecuteDataset(strConn, "prc_WEB_PAGE_NEWS_sel", spParams);
		}
		#endregion
		
		#region public DataSet Location_State_Select(int intCountryID)
		public DataSet Location_State_Select(int intCountryID)
		{
			spParams = new object[1];
			spParams.SetValue(intCountryID,	0);
			
			return data.SqlHelper.ExecuteDataset(this.strConn, "prc_LOCATION_STATE_sel", spParams);
		}
		#endregion
		
		#region public DataSet Location_Country_Select()
		public DataSet Location_Country_Select()
		{
			spParams = new object[0];
			
			return data.SqlHelper.ExecuteDataset(this.strConn, "prc_LOCATION_COUNTRY_sel", spParams);
		}
		#endregion
		
		#endregion
		
		#region clsContentManager properties
		
		public common.clsWeb	Web			{ get{return clsWeb;} }
		public string			MessageOut	{ get{return strMessage;} }
		
		#endregion
	}
}
