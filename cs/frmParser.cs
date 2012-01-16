using System;
using System.Collections;
using System.Configuration;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;
using System.Xml;

using ICICRYPT;
using Microsoft.ApplicationBlocks.Data;

namespace IMS.Conversion
{
	public class frmParser : System.Windows.Forms.Form
	{
		private Container				components = null;
		private ComboBox				cbbLocation,cbbNTLoginIDs;
		private ColumnHeader			colChkBox,colCaseKey,colCaseName,colDocGUID,colCheckoutStatusCd;
		private ListView				lv5701;
		private TextBox					txtLog;
		private SaveFileDialog			dlgSaveLog;
		private Button					btnParseOld5701s,btnSaveLog;
		private const string			strPairDelimiter	= "|";
		private string					strNew5701Doc		= Application.StartupPath + @"\Cases\ParsePilot5701sTemplate.xml";
		private	string					strDir5701sStart	= Application.StartupPath + @"\Cases";
		private	string					strLogFile			= Application.StartupPath + @"\Cases\ParsePilot5701s.log";
		
		private	string					strLocation,strConn,strConnPhase2Server,strUsername,strPassword,strDomain;
		private imssp3.IMSWS			clsWS_Pilot;
		private	imssp.IMSWS				clsWS_Phase2;
		
		[STAThread]
		static void Main() { Application.Run(new frmParser()); }
		
		#region frmParser constructor
		public frmParser()
		{
			InitializeComponent();
			
			clsWS_Pilot		= new imssp3.IMSWS();
			clsWS_Phase2	= new imssp.IMSWS();
		}
		#endregion
		
		// Step 0
		#region private void frmParser_Load(object sender, System.EventArgs e)
		private void frmParser_Load(object sender, System.EventArgs e)
		{
			// Set the default run location to Server
			cbbLocation.SelectedIndex	= 1;
			cbbLocation.Visible			= false;
			cbbNTLoginIDs.Visible		= false;
		}
		#endregion
		
		#region private string GetConnectionString(string strPathToConfigFile, string strAppSettingsKey)
		private string GetConnectionString(string strPathToConfigFile, string strAppSettingsKey)
		{
			try
			{
				ConfigXmlDocument	doc = new ConfigXmlDocument();
				doc.Load( strPathToConfigFile );
				
				XmlNode	nodeKey		= doc.SelectSingleNode("//appSettings/add[@key='" + strAppSettingsKey + "']");
				string	strEncValue	= nodeKey.Attributes["value"].Value.ToString();
				string	strDecValue	= ICICRYPT.Reversible.StrDecode(strEncValue,2026820330,true);
				
				// MessageBox.Show(strDecValue);
				
				SqlConnection objConn = new SqlConnection(strDecValue);
				objConn.Open();
				objConn.Close();
				objConn.Dispose();
				
				return strDecValue;
			}
			catch (System.Exception ex)
			{
				MessageBox.Show(this, "Error obtaining connection to " + strLocation + " database\n\n\"" + ex.Message + "\"\n" + ex.StackTrace, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
				this.Close();
			}
			
			return null;
		}
		#endregion
		
		#region private void cbbLocation_SelectedIndexChanged(object sender, System.EventArgs e)
		private void cbbLocation_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			strLocation = cbbLocation.Items[cbbLocation.SelectedIndex].ToString().Trim();
			
			switch (strLocation)
			{
				case "Client" : 
				{
					// Get the Client's MDSE_CONN_STRING value from startup dir's .config file
					this.strConn = this.GetConnectionString(Application.StartupPath + "\\IMS.exe.config", "MSDE_CONN_STRING");
					//MessageBox.Show(this.strConn);
				}
				break;
				case "Server" :
				{
					// Get the Server's .config ConnectionString value from file specified in modal form
					Form f = new frmServerWebConfigLoc();
					f.Text = strLocation + " Web.config";
					if (f.ShowDialog(this) == DialogResult.Cancel)
					{
						cbbLocation.SelectedIndex = 0;
						return;
					}
					
					this.strConnPhase2Server	= this.GetConnectionString(((frmServerWebConfigLoc)f).txtConfigLoc.Text, "ConnectionStringPhase2");
					this.strConn				= this.GetConnectionString(((frmServerWebConfigLoc)f).txtConfigLoc.Text, "ConnectionString");
					this.strUsername			= ((frmServerWebConfigLoc)f).txtUsername.Text.Trim();
					this.strPassword			= ((frmServerWebConfigLoc)f).txtUsername.Text.Trim();
					this.strDomain				= ((frmServerWebConfigLoc)f).txtDomain.Text.Trim();
					//MessageBox.Show(this.strConn + "\n" + this.strConnPhase2Server + "\n" + this.strUsername + "\n" + this.strDomain);
					
					// Create and connect to Pilot WebService
					clsWS_Pilot = new imssp3.IMSWS();
					clsWS_Pilot.Credentials								= System.Net.CredentialCache.DefaultCredentials;
					clsWS_Pilot.ConnectionGroupName						= "Group" + System.Security.Principal.WindowsIdentity.GetCurrent().Name;
					clsWS_Pilot.UnsafeAuthenticatedConnectionSharing	= true;
					
					clsWS_Phase2 = new imssp.IMSWS();
					clsWS_Phase2.Credentials							= System.Net.CredentialCache.DefaultCredentials;
					clsWS_Phase2.ConnectionGroupName					= "Group" + System.Security.Principal.WindowsIdentity.GetCurrent().Name;
					clsWS_Phase2.UnsafeAuthenticatedConnectionSharing	= true;
					
					/* 
					This doesn't work the same as CredentialCache.DefaultCredentials for some reason...
			
					System.Net.NetworkCredential auth = 
						new System.Net.NetworkCredential(
								this.strUsername
							,this.strPassword
							,this.strDomain
							);
								
					clsWS_Pilot.Credentials = auth;
					*/
				}
				break;
			}
			
			this.Bind5701NTLoginIDs();
			this.Bind5701List();
		}
		#endregion
		
		#region private void Bind5701NTLoginIDs()
		private void Bind5701NTLoginIDs()
		{
			try
			{
				cbbNTLoginIDs.DataSource	= SqlHelper.ExecuteDataset(this.strConn, CommandType.Text, "select distinct d.checkout_nt_login_id from vDOCUMENT_5701 d order by d.checkout_nt_login_id").Tables[0];
				cbbNTLoginIDs.ValueMember	= "checkout_nt_login_id";
				cbbNTLoginIDs.DisplayMember	= "checkout_nt_login_id";
				
				if (cbbNTLoginIDs.Items.Count == 0)
				{
					MessageBox.Show(this, "There are no NT LoginIDs associated\nwith the 5701 Forms in the " + strLocation + " database", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			catch (System.Exception ex)
			{
				MessageBox.Show(this, "Error getting the NT LoginIDs for\nthe 5701 Forms in the " + strLocation + " database\n\n" + ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		#endregion
		
		#region private void Bind5701List()
		private void Bind5701List()
		{
			if (cbbNTLoginIDs.Items.Count == 0) { return; }
			
			try
			{
				// Get enumerator of Cases living on the Client file system
				//		string[]	arrCases	= Directory.GetDirectories(strDir5701sStart);
				//		IEnumerator	IEnumCases	= arrCases.GetEnumerator();
				//		while (IEnumCases.MoveNext()) { ; }
				
				// Get Cases from db
				SqlDataReader drCases = SqlHelper.ExecuteReader(this.strConn, "PSP_GET_CASE_LIST", new object[0]);
				if (!drCases.HasRows)
				{
					MessageBox.Show(this, "No Cases exist in the " + strLocation + "  database", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
				
				lv5701.Items.Clear();
				while (drCases.Read())
				{
					// Set CaseKey, NTLoginID params
					string		strCaseDir		= strDir5701sStart + "\\" + drCases["case_system_identifier"].ToString();	//IEnumCases.Current.ToString();
					string		strCaseKey		= drCases["case_system_identifier"].ToString();								//strCaseDir.Substring(strCaseDir.LastIndexOf(@"\")+1);
					string		strCaseName		= drCases["name"].ToString();
					int			intCaseKey		= int.Parse(strCaseKey);
					string		strNTLoginID	= cbbNTLoginIDs.SelectedValue.ToString();
					//MessageBox.Show(intCaseKey.ToString());
					
					// (5) Hitting Phase2 IMS_LOCAL Client db
					// (4) Hitting Pilot IMS_EA Server db
					object[] spParams = new object[ ((strLocation=="Client")==true) ? 5 : 4 ];
					spParams.SetValue(intCaseKey,	0);
					spParams.SetValue("",			1);
					spParams.SetValue("",			2);
					spParams.SetValue("",			3);
					if (strLocation == "Client")
					{
					spParams.SetValue(strNTLoginID,	4);
					}
					
					// Get 5701s from db
					// select case_system_identifier,document_guid,checkout_status_cd,checkout_nt_login_id from vDOCUMENT_5701
					DataSet ds = SqlHelper.ExecuteDataset(this.strConn, "PSP_Get_5701_Document_Listing", spParams);
					if (ds.Tables.Count <= 0)
					{
						MessageBox.Show(this, "No 5701 Forms exist in the " + strLocation + " database", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}
					
					// Add ListView.Items
					foreach (DataRow dr in ds.Tables[0].Rows)
					{
						string	strDocGUID				= dr["document_guid"].ToString().Trim();
						string	strCheckoutStatusCd		= dr["checkout_status_cd"].ToString().ToUpper().Trim();
						string	strCheckoutNTLoginID	= dr["checkout_nt_login_id"].ToString().Trim();
						
						ListViewItem lvi = new ListViewItem("");
						lvi.SubItems.Add( intCaseKey.ToString() );
						lvi.SubItems.Add( strCaseName );
						lvi.SubItems.Add( strDocGUID );
						lvi.SubItems.Add( strCheckoutStatusCd );
						
						switch (strLocation)
						{
							case "Client" :
								// Documents are NEW or OUT and selected NTLoginID = CheckoutNTLoginID
								if (	strCheckoutStatusCd == "NEW" 
									|| (strCheckoutStatusCd == "OUT" && strCheckoutNTLoginID == strNTLoginID)
									)
								{
									lv5701.Items.Add( lvi );
								}
								break;
							case "Server" :
								// Documents are IN
								if (	strCheckoutStatusCd == "IN" 
									)
								{
									lv5701.Items.Add( lvi );
								}
								break;
						}
					}
				}
				
				// Default all ListView.Items to checked
				foreach (ListViewItem lvi in lv5701.Items) { lvi.Checked = true; }
			}
			catch (System.Exception ex)
			{
				MessageBox.Show(this, "Error getting the 5701 Forms from the " + strLocation + " database\n\n" + ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		#endregion
		
		// Step 1
		#region private void btnParseOld5701s_Click(object sender, System.EventArgs e)
		private void btnParseOld5701s_Click(object sender, System.EventArgs e)
		{
			// Create new StreamWriter for logging
			StreamWriter	logWriter	= File.CreateText(strLogFile);
							logWriter.WriteLine(strLocation.ToUpper() + " - " + this.Text + " Log - " + System.DateTime.Now.ToShortDateString());
						//	logWriter.WriteLine(strLocation.ToUpper() + " - " + cbbNTLoginIDs.SelectedValue.ToString());
							logWriter.WriteLine("---------------------------------------------------");
							logWriter.Flush();
			
			// Loop checked 5701.Items
			foreach (ListViewItem lvi in lv5701.Items)
			{
				if (lvi.Checked)
				{
					string strCaseKey	= lvi.SubItems[1].Text;
					string strDocGUID	= lvi.SubItems[3].Text;
					string strPath		= strDir5701sStart + "\\" + strCaseKey + "\\Issues";
					string strDoc		= strPath + "\\" + strDocGUID + ".xml";
					
					//	logWriter.WriteLine("PARSING CASE " + lvi.SubItems[1].Text.Trim() + @"\Issues 5701s");
					logWriter.WriteLine("PARSING " + ((strLocation == "Client")==true ? strDoc : strDocGUID + ".xml") + "...");
					logWriter.Flush();
					
					try
					{
						string			strXML	= null;
						StringBuilder	sbPairs	= new StringBuilder("");
						XmlDocument		doc		= new XmlDocument();
						
						switch (strLocation)
						{
							case "Client" :
								// Open, read, load the Infopath XML doc (ignore schema)
								StreamReader sr = File.OpenText( strDoc );
								strXML	= sr.ReadToEnd();
								sr.DiscardBufferedData();
								sr.Close();
								break;
							case "Server" : 
								byte[] arrDocBytes = clsWS_Pilot.GetLatestIssueDocument(long.Parse(strCaseKey), strDocGUID+".xml", strDocGUID, WindowsIdentity.GetCurrent().Name, "5701");
								strXML = Encoding.Default.GetString(arrDocBytes);
								
								// Getting some weird starting chars in this string...
								if (!strXML.StartsWith("<"))
								{
									strXML = strXML.Remove(0, strXML.IndexOf("<"));
								}
								break;
						}
						strXML = strXML.Replace("my:","");
						
						doc.LoadXml(strXML);
						//MessageBox.Show(doc.InnerXml.ToString());
						
						// Loop myFields Node
						XmlNodeList	fields = doc.ChildNodes[3].ChildNodes;
						IEnumerator IEnumNodes = fields.GetEnumerator();
						while (IEnumNodes.MoveNext())
						{
							XmlNodeList	children;
							XmlNode		node	= (XmlNode) IEnumNodes.Current;
							string		strKey	= node.Name;
							string		strVal	= "";
							
							switch (strKey)
							{
								case "GroupAdjustment" :
									children = node.ChildNodes[0].ChildNodes;
									sbPairs.Append( this.AppendChildNodePairs(children) );
									break;
								case "GroupApprovedBy" :
									children = node.ChildNodes[0].ChildNodes;
									sbPairs.Append( this.AppendChildNodePairs(children) );
									break;
								case "GroupMetaData" :
									children = node.ChildNodes;
									sbPairs.Append( this.AppendChildNodePairs(children) );
									break;
								default :
									strKey = node.Name;
									strVal = node.InnerText.Trim();
									sbPairs.Append( strKey + " = " + strVal + strPairDelimiter );
									break;
							}
						}
						
						// Remove trailing delimiter
						string	strPairs = sbPairs.ToString().Trim();
						strPairs = strPairs.Remove(strPairs.LastIndexOf(strPairDelimiter), 1);
						
						// Loop the split key/val array
						// Create new Hashtable of key/val
						string[]		arrPairs	= strPairs.Split(strPairDelimiter.ToCharArray());
						IEnumerator		IEnumPairs	= arrPairs.GetEnumerator();
						Hashtable		htPairs		= new Hashtable(arrPairs.GetUpperBound(0)+1);
						while (IEnumPairs.MoveNext())
						{
							string[]	arrPair		= IEnumPairs.Current.ToString().Trim().Split(new char[]{'='});
							string		strKey		= arrPair.GetValue(0).ToString().Trim();
							string		strVal		= (arrPair.GetUpperBound(0) > 0) ? arrPair.GetValue(1).ToString().Trim() : "";
					
							htPairs.Add(strKey, strVal);
						}
						
						// Save key/val pairs to db
						this.SaveParsed5701(
							 strDoc
							,htPairs
							,logWriter
							);
						
						logWriter.WriteLine("");
						logWriter.Flush();
					}
					catch (System.Exception ex)
					{
						// Log failure
						logWriter.WriteLine("FAILURE PARSING " + ((strLocation == "Client")==true ? strDoc : strDocGUID + ".xml") );
						logWriter.WriteLine(ex.Message + "\n" + ex.StackTrace);
						logWriter.WriteLine("");
						logWriter.Flush();
					}
				}
			}
			
			logWriter.Close();
			
			// Open log file, print to TextBox
			StreamReader logReader = File.OpenText(strLogFile);
			
			cbbNTLoginIDs.Visible		= false;
			lv5701.Visible				= false;
			btnParseOld5701s.Visible	= false;
			txtLog.Visible				= true;
			btnSaveLog.Visible			= true;
			txtLog.Text					= logReader.ReadToEnd();
			
			logReader.DiscardBufferedData();
			logReader.Close();
		}
		#endregion
		
		// Step 2
		#region private string AppendChildNodePairs(XmlNodeList nodes)
		private string AppendChildNodePairs(XmlNodeList nodes)
		{
			StringBuilder	sbPairs		= new StringBuilder("");
			IEnumerator		IEnumChild	= nodes.GetEnumerator();
			while (IEnumChild.MoveNext())
			{
				XmlNode nodeChild = (XmlNode)IEnumChild.Current;
				string	strKey	= nodeChild.Name;
				string	strVal	= nodeChild.InnerText.Trim();
				
				sbPairs.Append(strKey + " = " + strVal + strPairDelimiter);
			}
			
			return sbPairs.ToString();
		}
		#endregion
		
		// Step 3
		#region private void SaveParsed5701(string strDocPath, Hashtable htKeyVal, StreamWriter swForLog)
		private void SaveParsed5701(string strDocPath, Hashtable htKeyVal, StreamWriter swForLog)
		{
			// Print htKeyVal pairs
			/*
			IDictionaryEnumerator	IEnumHT	= htKeyVal.GetEnumerator();
			while (IEnumHT.MoveNext())
			{
				swForLog.WriteLine(	IEnumHT.Key.ToString() + " = " + IEnumHT.Value.ToString()	);
				swForLog.Flush();
			}
			*/
			
			// Write data to new 5701.xml doc
			XmlDocument		docNew		= new XmlDocument();
			string			strDocGUID	= strDocPath.Substring(strDocPath.LastIndexOf(@"\")+1).Replace(".xml","");
			
			switch (strLocation)
			{
				case "Client" :
					// Load old 5701 file
					docNew.Load( strDocPath );
					
					// Backup the old 5701.xml
					swForLog.WriteLine("MOVING " + strDocGUID+".xml to .bak");
					File.Move(strDocPath, strDocPath+".bak");
					File.SetAttributes(strDocPath+".bak", FileAttributes.ReadOnly);
					
					// Import new 5701.xml Infopath schema, overwrite old 5701.xml
					swForLog.WriteLine("COPYING template5701.xml to " + strDocGUID+".xml");
					File.Copy(strNew5701Doc, strDocPath);
					File.SetAttributes(strDocPath, FileAttributes.Normal);
					
					// Query SQL for the 5701 info just added??? (we already have the data in the Hashtable...)
					// ...
					break;
					
				case "Server" :
					// Read new 5701 template file
					docNew.Load(strNew5701Doc);
					
					// Need to save data to IMS_EA Phase2 db
					// this.strConn = "";
					break;
			}
			
			
			// Save to IMS_DOCUMENT table
			try
			{
				// Set AgreeStatus text
				string strAgree = null;
				if (bool.Parse(htKeyVal["AgreeinpartInd"].ToString()))	{ strAgree = "Agreed In Part"; }
				if (bool.Parse(htKeyVal["AgreeInd"].ToString()))		{ strAgree = "Agree"; }
				if (bool.Parse(htKeyVal["DisagreeInd"].ToString()))		{ strAgree = "Disagreed"; }
				
				object[] spParams = new object[7];
				spParams.SetValue(htKeyVal["Document_GUID"].ToString(),		0);
				spParams.SetValue(htKeyVal["Current_User"].ToString(),		1);
				spParams.SetValue(htKeyVal["Issue_Description"].ToString(),	2);
				spParams.SetValue(htKeyVal["IssueNumber"].ToString(),		3);
				spParams.SetValue(htKeyVal["IssueDate"].ToString(),			4);
				spParams.SetValue(htKeyVal["ResponseDate"].ToString(),		5);
				spParams.SetValue(strAgree,									6);
				
				switch (strLocation)
				{
					case "Client" : 
						SqlHelper.ExecuteNonQuery(this.strConn, "PSP_Update_Issue_5701", spParams);
						break;
					case "Server" : 
						SqlHelper.ExecuteNonQuery(this.strConnPhase2Server, "PSP_Update_Issue_5701", spParams);
						break;
				}
				
				swForLog.WriteLine("\tSUCCESS Exec PSP_Update_Issue_5701("+htKeyVal["Document_GUID"].ToString()+")");
			}
			catch (System.Exception ex)
			{
				swForLog.WriteLine("\tFAILURE Exec PSP_Update_Issue_5701("+htKeyVal["Document_GUID"].ToString()+")");
				swForLog.WriteLine("\t"+ex.Message);
			}
			
			// Save to IMS_DOCUMENT_DEMOGRAPHIC table
			// (Note: None of these fields (strValueCode) existed in the Pilot 5701 Infopath xml docs, but are being saved in Phase2)
			this.SaveDocumentDemographic(htKeyVal, "TaxPayerID",			"TaxpayerContactName",	swForLog);
			this.SaveDocumentDemographic(htKeyVal, "CarryOver",				"CarryoverAdjustment",	swForLog);
			this.SaveDocumentDemographic(htKeyVal, "AdjustmentClaim",		"AdjustmentClaim",		swForLog);
			this.SaveDocumentDemographic(htKeyVal, "SubCarryOver",			"SubsequentCarryover",	swForLog);
			this.SaveDocumentDemographic(htKeyVal, "TaxPayerAction",		"TaxpayerAction",		swForLog);
			this.SaveDocumentDemographic(htKeyVal, "SubmitInfo",			"SubmitInformation",	swForLog);
			this.SaveDocumentDemographic(htKeyVal, "5701_Reason_Unagreed",	"ReasonUnagreed",		swForLog);
			
			swForLog.WriteLine("");
			
			// Set new 5701.xml field values
			docNew.ChildNodes[3]["my:Tin"].InnerText				= htKeyVal["Tin"].ToString();
			docNew.ChildNodes[3]["my:IssueNumber"].InnerText		= htKeyVal["IssueNumber"].ToString();
			docNew.ChildNodes[3]["my:IssueDate"].InnerText			= htKeyVal["IssueDate"].ToString();
			docNew.ChildNodes[3]["my:NameAndAddress"].InnerText		= htKeyVal["NameAndAddress"].ToString();
			docNew.ChildNodes[3]["my:EntityName"].InnerText			= htKeyVal["EntityName"].ToString();
			docNew.ChildNodes[3]["my:EntityNumber"].InnerText		= htKeyVal["EntityNumber"].ToString();
			docNew.ChildNodes[3]["my:ResponseDate"].InnerText		= htKeyVal["ResponseDate"].ToString();
			docNew.ChildNodes[3]["my:ProposedBy"].InnerText			= htKeyVal["ProposedBy"].ToString();
			docNew.ChildNodes[3]["my:ProposedById"].InnerText		= htKeyVal["ProposedById"].ToString();
			docNew.ChildNodes[3]["my:ProposedByTitle"].InnerText	= htKeyVal["ProposedByTitle"].ToString();
			docNew.ChildNodes[3]["my:SubmittedTo"].InnerText		= htKeyVal["SubmittedTo"].ToString();
			docNew.ChildNodes[3]["my:submittedToTitle"].InnerText	= htKeyVal["submittedToTitle"].ToString();
			docNew.ChildNodes[3]["my:AgreeInd"].InnerText			= htKeyVal["AgreeInd"].ToString();
			docNew.ChildNodes[3]["my:AgreeinpartInd"].InnerText		= htKeyVal["AgreeinpartInd"].ToString();
			docNew.ChildNodes[3]["my:DisagreeInd"].InnerText		= htKeyVal["DisagreeInd"].ToString();
			docNew.ChildNodes[3]["my:AdditionalInfoInd"].InnerText	= htKeyVal["AdditionalInfoInd"].ToString();
			docNew.ChildNodes[3]["my:WillWubmitByDate"].InnerText	= htKeyVal["WillWubmitByDate"].ToString();
			docNew.ChildNodes[3]["my:AuthorizedTitle"].InnerText	= htKeyVal["AuthorizedTitle"].ToString();
			docNew.ChildNodes[3]["my:AuthorizedDate"].InnerText		= htKeyVal["AuthorizedDate"].ToString();
			docNew.ChildNodes[3]["my:Details"].InnerText			= htKeyVal["Details"].ToString();
			docNew.ChildNodes[3]["my:SAIN"].InnerText				= htKeyVal["SAIN"].ToString();
			docNew.ChildNodes[3]["my:UIL"].InnerText				= htKeyVal["UIL"].ToString();
			docNew.ChildNodes[3]["my:GroupAdjustment"]["my:GroupAdjustmentRecord"]["my:IssueYear"].InnerText		= htKeyVal["IssueYear"].ToString();
			docNew.ChildNodes[3]["my:GroupAdjustment"]["my:GroupAdjustmentRecord"]["my:IssueRaised"].InnerText		= htKeyVal["IssueRaised"].ToString();
			docNew.ChildNodes[3]["my:GroupAdjustment"]["my:GroupAdjustmentRecord"]["my:IssueCategory"].InnerText	= htKeyVal["IssueCategory"].ToString();
			docNew.ChildNodes[3]["my:GroupAdjustment"]["my:GroupAdjustmentRecord"]["my:IssueAmount"].InnerText		= htKeyVal["IssueAmount"].ToString();
			docNew.ChildNodes[3]["my:GroupApprovedBy"]["my:GroupApprovedByRecord"]["my:ApprovedName"].InnerText		= htKeyVal["ApprovedName"].ToString();
			docNew.ChildNodes[3]["my:GroupApprovedBy"]["my:GroupApprovedByRecord"]["my:ApprovedTitle"].InnerText	= htKeyVal["ApprovedTitle"].ToString();
			docNew.ChildNodes[3]["my:GroupApprovedBy"]["my:GroupApprovedByRecord"]["my:ApprovedDate"].InnerText		= htKeyVal["ApprovedDate"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:Document_Type_CD"].InnerText			= htKeyVal["Document_Type_CD"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:Case_System_Identifier"].InnerText		= htKeyVal["Case_System_Identifier"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:Task_Sequence_Number"].InnerText		= htKeyVal["Task_Sequence_Number"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:Create_NT_Login_ID"].InnerText			= htKeyVal["Create_NT_Login_ID"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:Date_Created"].InnerText				= htKeyVal["Date_Created"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:Last_Updated_NT_Login_ID"].InnerText	= htKeyVal["Last_Updated_NT_Login_ID"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:Date_Updated"].InnerText				= htKeyVal["Date_Updated"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:Date_Last_Synchronized"].InnerText		= htKeyVal["Date_Last_Synchronized"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:Checkout_Status_CD"].InnerText			= htKeyVal["Checkout_Status_CD"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:Connection"].InnerText					= htKeyVal["Connection"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:Edit_Mode"].InnerText					= htKeyVal["Edit_Mode"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:Current_User"].InnerText				= htKeyVal["Current_User"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:Checkout_NT_Login_ID"].InnerText		= htKeyVal["Checkout_NT_Login_ID"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:Document_Number"].InnerText			= htKeyVal["Document_Number"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:Document_Bytes"].InnerText				= htKeyVal["Document_Bytes"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:Issue_Description"].InnerText			= htKeyVal["Issue_Description"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:Document_Issue_Date"].InnerText		= htKeyVal["Document_Issue_Date"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:PIR_Sequence_Number"].InnerText		= htKeyVal["PIR_Sequence_Number"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:Document_Name"].InnerText				= htKeyVal["Document_Name"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:Document_GUID"].InnerText				= htKeyVal["Document_GUID"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:Document_Status"].InnerText			= htKeyVal["Document_Status"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:Document_Status_Date"].InnerText		= htKeyVal["Document_Status_Date"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:Synchronize_Ind"].InnerText			= htKeyVal["Synchronize_Ind"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:Issue_System_Identifier"].InnerText	= htKeyVal["Issue_System_Identifier"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:Document_Received_Date"].InnerText		= htKeyVal["Document_Received_Date"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:Response_Date"].InnerText				= htKeyVal["Response_Date"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:Infopath_Forms_Directory"].InnerText	= htKeyVal["Infopath_Forms_Directory"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:IMS_App_Path"].InnerText				= htKeyVal["IMS_App_Path"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:Cycle_Start_Year"].InnerText			= htKeyVal["Cycle_Start_Year"].ToString();
			docNew.ChildNodes[3]["my:GroupMetaData"]["my:Cycle_End_Year"].InnerText				= htKeyVal["Cycle_End_Year"].ToString();
		//	These fields are not in the old 5701.xml doc...
		//	docNew.ChildNodes[3]["my:GroupMetaData"]["my:ITC_Identifier"].InnerText				= htKeyVal["ITC_Identifier"].ToString();
		//	docNew.ChildNodes[3]["my:GroupMetaData"]["my:ReferenceNumber"].InnerText			= htKeyVal["ReferenceNumber"].ToString();
		//	docNew.ChildNodes[3]["my:GroupMetaData"]["my:CustomWorksheet"].InnerText			= htKeyVal["CustomWorksheet"].ToString();
			
			switch (strLocation)
			{
				case "Client" :
					// Save new 5701.xml back to old 5701.xml file system location
					docNew.Save(strDocPath);
					break;
					
				case "Server" : 
					// Get bytes from populated XML doc object
					byte[] arrNewDocBytes = Encoding.Default.GetBytes( docNew.InnerXml );
					//MessageBox.Show( arrNewDocBytes.Length.ToString() );
					
					// Get old doc bytes
					// clsWS_Phase2.CheckOutIssueDocument(
					byte[] arrOldDocBytes = 
						clsWS_Pilot.CheckOutIssueDocument(
						long.Parse(htKeyVal["Case_System_Identifier"].ToString())
						,strDocGUID+".xml"
						,strDocGUID
						,htKeyVal["Create_NT_Login_ID"].ToString()
						,"5701"
						);
					
					// Get DataSet to check in
					// Must pass only one issue at a time
					DataSet			dsCheckIn	= new DataSet();
					SqlParameter[]	param		= new SqlParameter[1];
									param[0]	= new SqlParameter("@p_Document_GUID", strDocGUID);
					DataSet			dsIssues	= SqlHelper.ExecuteDataset(this.strConnPhase2Server, "PSP_Get_IMS_Document_Status_History", param);
					DataTable		dtbl1		= dsIssues.Tables[0].Copy();
					// Issues is in first table in dataset
					dtbl1.TableName = "Status_History";
					dsCheckIn.Tables.Add(dtbl1);
					
					// Check in new doc bytes, DataSet
					// clsWS_Phase2.CheckInIssueDocument(
					clsWS_Pilot.CheckInIssueDocument(
						long.Parse(htKeyVal["Case_System_Identifier"].ToString())
						,strDocGUID+".xml"
						,false
						,htKeyVal["Create_NT_Login_ID"].ToString()
						,arrNewDocBytes
						,dsCheckIn
						,htKeyVal["Issue_System_Identifier"].ToString()
						,"5701"
						);
						
					break;
			}
			
			swForLog.WriteLine("");
		}
		#endregion
		
		// Step 3.5
		#region private void SaveDocumentDemographic(Hashtable htKeyVal, string strCode, string strValueCode, StreamWriter swForLog)
		private void SaveDocumentDemographic(Hashtable htKeyVal, string strCode, string strValueCode, StreamWriter swForLog)
		{
			try
			{
				// SaveDocumentDemographic(Hashtable htKeyVal, string strCode, string strValueCode)
				string	strDocGUID = htKeyVal["Document_GUID"].ToString();
				strValueCode = (htKeyVal.ContainsKey(strValueCode)) ? htKeyVal[strValueCode].ToString() : null;
				
				object[] spParams = new object[6];
				spParams.SetValue(strDocGUID,	0);
				spParams.SetValue(strCode,		1);
				spParams.SetValue(strValueCode,	2);
				spParams.SetValue(null,			3);
				spParams.SetValue(null,			4);
				spParams.SetValue(null,			5);
				
				switch (strLocation)
				{
					case "Client" : 
						SqlHelper.ExecuteNonQuery(this.strConn, "PSP_Save_IMS_Document_Demographic", spParams);
						break;
					case "Server" : 
						SqlHelper.ExecuteNonQuery(this.strConnPhase2Server, "PSP_Save_IMS_Document_Demographic", spParams);
						break;
				}
				
				swForLog.WriteLine("\tSUCCESS Exec PSP_Save_IMS_Document_Demographic(code='"+strCode+"')");
			}
			catch (System.Exception ex)
			{
				swForLog.WriteLine("\tFAILURE Exec PSP_Save_IMS_Document_Demographic(code='"+strCode+"')");
				swForLog.WriteLine("\t"+ex.Message);
			}
		}
		#endregion
		
		// Step 4
		#region private void btnSaveLog_Click(object sender, System.EventArgs e)
		private void btnSaveLog_Click(object sender, System.EventArgs e)
		{
			FileInfo fInfo	= new FileInfo(strLogFile);
			
			dlgSaveLog.FileName			= fInfo.Name.Substring(0,fInfo.Name.IndexOf(".")) + "_" + System.Environment.MachineName.ToUpper() + ".log";
			dlgSaveLog.InitialDirectory	= strDir5701sStart;
			
			if (dlgSaveLog.ShowDialog() == DialogResult.OK)
			{
				File.Copy(strLogFile, dlgSaveLog.FileName, true);
			}
		}
		#endregion
		
		#region protected override void Dispose( bool disposing )
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}
		#endregion
		
		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.btnParseOld5701s = new System.Windows.Forms.Button();
			this.lv5701 = new System.Windows.Forms.ListView();
			this.colChkBox = new System.Windows.Forms.ColumnHeader();
			this.colCaseKey = new System.Windows.Forms.ColumnHeader();
			this.colDocGUID = new System.Windows.Forms.ColumnHeader();
			this.colCheckoutStatusCd = new System.Windows.Forms.ColumnHeader();
			this.cbbNTLoginIDs = new System.Windows.Forms.ComboBox();
			this.txtLog = new System.Windows.Forms.TextBox();
			this.btnSaveLog = new System.Windows.Forms.Button();
			this.dlgSaveLog = new System.Windows.Forms.SaveFileDialog();
			this.cbbLocation = new System.Windows.Forms.ComboBox();
			this.colCaseName = new System.Windows.Forms.ColumnHeader();
			this.SuspendLayout();
			// 
			// btnParseOld5701s
			// 
			this.btnParseOld5701s.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnParseOld5701s.Location = new System.Drawing.Point(8, 241);
			this.btnParseOld5701s.Name = "btnParseOld5701s";
			this.btnParseOld5701s.Size = new System.Drawing.Size(136, 23);
			this.btnParseOld5701s.TabIndex = 0;
			this.btnParseOld5701s.Text = "Parse Pilot 5701 Forms";
			this.btnParseOld5701s.Click += new System.EventHandler(this.btnParseOld5701s_Click);
			// 
			// lv5701
			// 
			this.lv5701.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.lv5701.CheckBoxes = true;
			this.lv5701.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
																					 this.colChkBox,
																					 this.colCaseKey,
																					 this.colCaseName,
																					 this.colDocGUID,
																					 this.colCheckoutStatusCd});
			this.lv5701.FullRowSelect = true;
			this.lv5701.GridLines = true;
			this.lv5701.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.lv5701.Location = new System.Drawing.Point(9, 34);
			this.lv5701.Name = "lv5701";
			this.lv5701.Size = new System.Drawing.Size(475, 200);
			this.lv5701.TabIndex = 3;
			this.lv5701.View = System.Windows.Forms.View.Details;
			// 
			// colChkBox
			// 
			this.colChkBox.Text = "";
			this.colChkBox.Width = 20;
			// 
			// colCaseKey
			// 
			this.colCaseKey.Text = "Case Key";
			this.colCaseKey.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// colDocGUID
			// 
			this.colDocGUID.Text = "Document GUID";
			this.colDocGUID.Width = 150;
			// 
			// colCheckoutStatusCd
			// 
			this.colCheckoutStatusCd.Text = "Status";
			this.colCheckoutStatusCd.Width = 50;
			// 
			// cbbNTLoginIDs
			// 
			this.cbbNTLoginIDs.Location = new System.Drawing.Point(9, 7);
			this.cbbNTLoginIDs.Name = "cbbNTLoginIDs";
			this.cbbNTLoginIDs.Size = new System.Drawing.Size(187, 21);
			this.cbbNTLoginIDs.TabIndex = 4;
			// 
			// txtLog
			// 
			this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.txtLog.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.txtLog.Location = new System.Drawing.Point(9, 34);
			this.txtLog.Multiline = true;
			this.txtLog.Name = "txtLog";
			this.txtLog.ReadOnly = true;
			this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.txtLog.Size = new System.Drawing.Size(475, 200);
			this.txtLog.TabIndex = 5;
			this.txtLog.Text = "";
			this.txtLog.Visible = false;
			this.txtLog.WordWrap = false;
			// 
			// btnSaveLog
			// 
			this.btnSaveLog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnSaveLog.Location = new System.Drawing.Point(8, 241);
			this.btnSaveLog.Name = "btnSaveLog";
			this.btnSaveLog.Size = new System.Drawing.Size(136, 23);
			this.btnSaveLog.TabIndex = 6;
			this.btnSaveLog.Text = "Save Log";
			this.btnSaveLog.Visible = false;
			this.btnSaveLog.Click += new System.EventHandler(this.btnSaveLog_Click);
			// 
			// dlgSaveLog
			// 
			this.dlgSaveLog.Filter = "Log files (*.log)|*.log|Text files (*.txt)|*.txt";
			// 
			// cbbLocation
			// 
			this.cbbLocation.Items.AddRange(new object[] {
															 "Client",
															 "Server"});
			this.cbbLocation.Location = new System.Drawing.Point(364, 7);
			this.cbbLocation.Name = "cbbLocation";
			this.cbbLocation.Size = new System.Drawing.Size(121, 21);
			this.cbbLocation.TabIndex = 7;
			this.cbbLocation.SelectedIndexChanged += new System.EventHandler(this.cbbLocation_SelectedIndexChanged);
			// 
			// colCaseName
			// 
			this.colCaseName.Text = "Name";
			this.colCaseName.Width = 100;
			// 
			// frmParser
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(492, 273);
			this.Controls.Add(this.cbbLocation);
			this.Controls.Add(this.cbbNTLoginIDs);
			this.Controls.Add(this.btnParseOld5701s);
			this.Controls.Add(this.btnSaveLog);
			this.Controls.Add(this.lv5701);
			this.Controls.Add(this.txtLog);
			this.MaximumSize = new System.Drawing.Size(1024, 768);
			this.MinimumSize = new System.Drawing.Size(500, 300);
			this.Name = "frmParser";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.Text = "Parse Pilot 5701 Forms";
			this.Load += new System.EventHandler(this.frmParser_Load);
			this.ResumeLayout(false);

		}
		#endregion
	}
}
