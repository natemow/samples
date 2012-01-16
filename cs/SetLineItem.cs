using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Configuration;
using System.Xml;
using System.Text.RegularExpressions;
using System.Xml.Schema;
using System.Data.SqlClient;
using Microsoft.ApplicationBlocks.Data;

using BNA.CTAA.TaxEngine;
using BNA.CTAA.TaxEngine.resources;

namespace IMS.Forms
{
	public class SetLineItem : System.Windows.Forms.Form
	{
		private Button		cmdCustom, cmdCancel,cmdSave;
		private Label		lbllabel1,lbllabel2,lbllabel3,label1;
		private TextBox		txtGuide;
		public	TextBox		txtCustomWorksheet,txtReferenceNumber;
		private TreeView	tvwForms;
		private Panel		pnlPrompts,pnlImgLights;
		private Container	components = null;	// Required designer variable
		private int			mintExaminationYear = 0;
		private	string		m_doc_guid;
		private string		m_preselect_RefPoint, m_preselect_CustomWorksheet, m_preselect_Prompts;
		private	long		m_case_pkey;
		
		private	string		strLineName	= "";
		private	string		strLineDesc	= "";
		
		enum IMAGE { BNA, PROMPT, IRS, EMPTY };
		
		#region SetLineItem constructors
		public SetLineItem(int pintExaminationYear)
		{
			InitializeComponent();
			
			mintExaminationYear	= pintExaminationYear;
			m_doc_guid			= null;
			
			LoadSupportedForms (mintExaminationYear);
		}
		
		public SetLineItem(int pintExaminationYear, long longCaseKey, string strDocGUID, string strTaxEngineReferencePoint, string strCustomWorksheet)
		{
			InitializeComponent();
			
			mintExaminationYear			= pintExaminationYear;
			m_doc_guid					= strDocGUID;
			m_preselect_RefPoint		= strTaxEngineReferencePoint;
			m_preselect_CustomWorksheet	= strCustomWorksheet;
			m_case_pkey					= longCaseKey;
			
			LoadSupportedForms (mintExaminationYear);
		}
		#endregion
		
		
		#region protected override void Dispose( bool disposing )
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}
		#endregion
		
		#region private void LoadSupportedForms (int pintExaminationYear)
		private void LoadSupportedForms (int pintExaminationYear)
		{
			tvwForms.Nodes.Clear ();
			ImageList il = new ImageList ();
			
			il.Images.Add ( Image.FromFile ( Application.StartupPath + "\\Images\\bna.gif" ) );
			il.Images.Add ( Image.FromFile ( Application.StartupPath + "\\Images\\prompt.gif" ) );
			il.Images.Add ( Image.FromFile ( Application.StartupPath + "\\Images\\irs.gif" ) );
			il.Images.Add ( Image.FromFile ( Application.StartupPath + "\\Images\\empty.gif" ) );
			
			tvwForms.ImageList = il;
			tvwForms.ImageIndex = 2;
			tvwForms.SelectedImageIndex = 2;
			
			XmlDocument doc = new XmlDocument ();
			try
			{
				//SupportedForms forms = new SupportedForms();
				//doc.Load ( "C:\\Projects\\BNA\\supportedforms\\supportedFormsSample.xml" );
				//int taxYear = 1999;

				doc = BNA.CTAA.TaxEngine.SupportedForms.GetSupportedForms(pintExaminationYear);
				
				XmlValidatingReader validatedForm = new XmlValidatingReader(doc.OuterXml, XmlNodeType.Document, null);
				
				validatedForm.Schemas.Add(SchemaResources.SupportedForms);
				validatedForm.ValidationType = ValidationType.Schema;
				validatedForm.ValidationEventHandler += new ValidationEventHandler(this.ValidationEvent);
				
				while(validatedForm.Read()){};
				
				// Write doc to file for debug...
				/*
				System.Xml.XmlTextWriter writer = new XmlTextWriter(@"C:\Projects\IMS\Phase_2\Client\IMS\Forms\SetLineItemGetSupportedForms.xml", System.Text.Encoding.ASCII);
				writer.WriteRaw(doc.InnerXml);
				writer.Flush();
				writer.Close();
				*/
				
				// Pre-populate tree node selection and Path field
				this.SetNodeAttributesToMatchOn( doc );
				
				// Pre-populate Custom Worksheet field
				txtCustomWorksheet.Text	= m_preselect_CustomWorksheet;
				
				this.ConvertXmlNodeToTreeNode ( doc, tvwForms.Nodes );
			}
			catch (InvalidTaxYearException exYear)
			{
				MessageBox.Show(this, "Form 5701 for this tax period will not automatically post to the tax computation software.\nAdjustments for this tax period will have to be manually entered by the user.", "Set Tax Form Line Number");
				this.Close();
			}
			catch ( Exception ex )
			{
				//MessageBox.Show ( ex.Message );
				MessageBox.Show(this, ex.Message, "Set Tax Form Line Number");
				this.Close();
			}
		}
		#endregion
		
		#region private void SetNodeAttributesToMatchOn(XmlDocument targetDoc)
		private void SetNodeAttributesToMatchOn(XmlDocument targetDoc)
		{
			txtReferenceNumber.Text = m_preselect_RefPoint;
			
			// Pre-select the tree node if value was passed into the constructor
			string strXPathExp		= "";
			string strPromptPairs	= "";
			
			try
			{
				if (m_preselect_RefPoint.ToString().Length==0) { m_preselect_RefPoint = null; }
				if (m_preselect_RefPoint == null) { return; }
					
				// This is the saved value from txtReferenceNumber.Text
				// i.e. m_preselect_RefPoint = "/IRS Form 1120/Line 14"
				
				// Create an XPath expression to get the description attribute value from doc 
				// so we can match and pre-select the saved tree node
				
				int			ixTreeDepth	= 0;
				string[]	arrPath		= m_preselect_RefPoint.Substring(1).Split(new char[]{'/'});
				IEnumerator	IEnumPath	= arrPath.GetEnumerator();
				while (IEnumPath.MoveNext())
				{
					string strCurrent = IEnumPath.Current.ToString();
					
					// Need to exclude any Prompts previously appended
					// in the cmdSave event from the XPath string
					if (!strCurrent.StartsWith("|") && !strCurrent.EndsWith("|"))
					{	
						if (ixTreeDepth==0)
						{
							strXPathExp += "//Form[@name=\"" + strCurrent + "\"]/Lines/";
						}
						if (ixTreeDepth==1)
						{
							strXPathExp += "Line[@name=\"" + strCurrent + "\"]";
						}
						if (ixTreeDepth >1)
						{
							strXPathExp += "/TaxEngineLine[@name=\"" + strCurrent+ "\"]";
						}
					
						ixTreeDepth++;
					}
					// Append string of Prompt key/val pairs
					else
					{
						// Remove prepended "|" char
						strPromptPairs += strCurrent.Remove(0,1);
					}
				}
				
				//MessageBox.Show( targetDoc.InnerXml.ToString() );
				//MessageBox.Show( strXPathExp );
				XmlNode nodeSelect = targetDoc.SelectSingleNode( strXPathExp );
				
				switch(nodeSelect.Name)
				{
					case "Line" : 
						strLineName		= nodeSelect.Attributes["name"].Value;
						strLineDesc		= nodeSelect.Attributes["description"].Value;
						break;
					case "Form" :
						strLineName		= nodeSelect.Attributes["name"].Value;
						strLineDesc		= "";
						break;
					case "TaxEngineLine" :
						strLineName		= nodeSelect.Attributes["name"].Value;
						strLineDesc		= "";
						break;
				}
				
				//MessageBox.Show(strLineName + "    [" + strLineDesc + "]");
				
				// Split this string later in PostPrompts() to pre-fill Prompt textboxes
				if (strPromptPairs.Length > 0)
				{
					// Remove trailing "|" char
					m_preselect_Prompts = strPromptPairs.Remove(strPromptPairs.Length-1,1);
				}
			}
			catch (System.Exception ex)
			{
				Logger.Log(LogLevel.Debug,"SetLineItem.SetNodeAttributesToMatchOn","Error pre-selecting tree node.\n XPath expression built: " + strXPathExp + "\n" + strLineDesc + "\n" + ex.ToString() + " . " );
			}
		}
		#endregion
		
		#region public void ConvertXmlNodeToTreeNode ( XmlNode xmlNode, TreeNodeCollection treeNodes )
		public void ConvertXmlNodeToTreeNode ( XmlNode xmlNode, TreeNodeCollection treeNodes )
		{
			try
			{
				TreeNode newTreeNode = null;
				TreeNode tempTreeNode = new TreeNode ();
				switch ( xmlNode.Name )
				{
					case "Line":
						XmlAttribute lineName = (XmlAttribute)xmlNode.Attributes.Item(0);
						XmlAttribute lineDesc = (XmlAttribute)xmlNode.Attributes.Item(1);
						tempTreeNode.ImageIndex = 0;
						tempTreeNode.SelectedImageIndex = 0;
						//tempTreeNode.Text = "Line " + lineName.Value + "    [" + lineDesc.Value + "]";
						tempTreeNode.Text = lineName.Value + "    [" + lineDesc.Value + "]";
						treeNodes.Add ( tempTreeNode );
						newTreeNode = tempTreeNode;
						
						// Check for pre-selection of this node (see this.SetNodeAttributesToMatchOn() method)
						if (	lineName.Value == strLineName
							&&	lineDesc.Value == strLineDesc)
						{
							newTreeNode.TreeView.SelectedNode = tempTreeNode;
						}
						
						break;
					case "Form":
						XmlNode childNode = xmlNode.ChildNodes[0];
						XmlAttribute formName = (XmlAttribute)xmlNode.Attributes.Item(0);
						tempTreeNode.ImageIndex = 2;
						tempTreeNode.SelectedImageIndex = 2;
						tempTreeNode.Text = formName.Value;
						treeNodes.Add ( tempTreeNode );
						newTreeNode = tempTreeNode;
						break;
					case "TaxEngineLine":
						XmlAttribute taxenginelineName = (XmlAttribute)xmlNode.Attributes.Item(0);
						tempTreeNode.ImageIndex = 0;
						tempTreeNode.SelectedImageIndex = 0;
						tempTreeNode.Text = ((XmlAttribute)xmlNode.Attributes.Item(0)).Value;
						treeNodes.Add ( tempTreeNode );
						//if( xmlNode.Attributes.Count == 3 )
						if( xmlNode.Attributes.Count >=2 )
						{
							TreeNode tempChildNode = tempTreeNode.Nodes.Add ( ((XmlAttribute)xmlNode.Attributes.Item(1)).Value );
							tempChildNode.ImageIndex = 1;
							tempChildNode.SelectedImageIndex = 1;
						}
						newTreeNode = tempTreeNode;
						
						// Check for pre-selection of this node (see this.SetNodeAttributesToMatchOn() method)
						if ( taxenginelineName.Value == strLineName )
						{
							newTreeNode.TreeView.SelectedNode = tempTreeNode;
						}
						
						break;
					case "TaxEnginePrompt":
						XmlAttribute promptCaption = (XmlAttribute)xmlNode.Attributes.Item(0);
						tempTreeNode.ImageIndex = 1;
						tempTreeNode.SelectedImageIndex = 1;
						tempTreeNode.Text = promptCaption.Value;
						treeNodes.Add ( tempTreeNode );
						newTreeNode = tempTreeNode;
						break;
					default:
						break;
				}
				foreach ( XmlNode childNode in xmlNode.ChildNodes )
				{
					if (null == newTreeNode)
					{
						ConvertXmlNodeToTreeNode( childNode, treeNodes );
					}
					else
					{
						ConvertXmlNodeToTreeNode( childNode, newTreeNode.Nodes );
					}
				}

				
			}
			catch (Exception ex)
			{
				Logger.Log(LogLevel.Error,"SetLineItem.ConvertXMLNodeToTreeNode","Error " + ex.ToString());
			}
		}
		#endregion
		
		#region private void PostPrompts(TreeNode pNode)
		private void PostPrompts(TreeNode pNode)
		{
			int intStartTabIndex = pnlPrompts.TabIndex;
			
			int lHeight=10;
			int lCount=1;
			TreeNode ChildNode = null;
			try
			{
				pnlPrompts.Controls.Clear();
				if (pNode.ImageIndex == (int)IMAGE.PROMPT)
				{
					//Show the prompts and allow to save if all prompts are entered
					
					Label lbl = new Label();
					lbl.Text = pNode.Text;
					lbl.Location = new Point(10,lHeight);
					lbl.Width = pnlPrompts.Width;
					lbl.Height = lHeight+5;
					//lbl.Width = lbl.Text.Length+10;
					
					TextBox txt = new TextBox();
					txt.Name = lCount.ToString();
					txt.Tag = lbl.Text;
					txt.Width = pnlPrompts.Width;
					txt.Location = new Point(0, lbl.Location.Y*3);
					txt.Size = txtReferenceNumber.Size;
					txt.TabIndex = intStartTabIndex+1;
					
					pnlPrompts.Controls.Add( lbl);
					pnlPrompts.Controls.Add( txt);
					
					ChildNode = pNode;
					//				if (ChildNode.NextNode != null && ChildNode.NextNode.ImageIndex != (int) IMAGE.PROMPT)
					//				{
					//					lbPromptPrefix = true;
					//				}
					while (ChildNode.NextNode != null && ChildNode.NextNode.ImageIndex == (int) IMAGE.PROMPT)
					{
						intStartTabIndex = intStartTabIndex+1;
						
						lCount = lCount+ 1;
						lHeight = lHeight + lbl.Height+ txt.Height + 10;
						lbl = new Label();
						lbl.Text = ChildNode.NextNode.Text;
						lbl.Location = new Point(10,lHeight);
						lbl.Width = pnlPrompts.Width;
						//lbl.Height = lHeight+5;
						
						txt = new TextBox();
						txt.Name = lCount.ToString();
						txt.Tag = lbl.Text;
						txt.Width = pnlPrompts.Width;
						txt.Location = new Point(0, lbl.Location.Y + lbl.Height +3);
						txt.Size = txtReferenceNumber.Size;
						txt.TabIndex = intStartTabIndex;
						
						pnlPrompts.Controls.Add( lbl);
						pnlPrompts.Controls.Add( txt);
						ChildNode = ChildNode.NextNode;
						//					if (ChildNode.NextNode != null && ChildNode.NextNode.ImageIndex != (int) IMAGE.PROMPT)
						//					{
						//						lbPromptPrefix = true;
						//					}
					}
					
					#region Pre-fill Prompt TextBoxes with previously saved values
					
					// See this.SetNodeAttributesToMatchOn for creation of m_preselect_Prompts
					if (m_preselect_Prompts != null)
					{
						// Loop the previously saved Prompt key/val pairs
						IEnumerator IEnumPrompts = m_preselect_Prompts.Split(new char[]{'|'}).GetEnumerator();
						while (IEnumPrompts.MoveNext())
						{
							string[]	arrPromptPair	= IEnumPrompts.Current.ToString().Split(new char[]{','});
							string		strKey			= arrPromptPair.GetValue(0).ToString();
							string		strVal			= arrPromptPair.GetValue(1).ToString();
							//MessageBox.Show( strKey + " = " + strVal );
							
							// Check for a match on strKey = pnlPrompts.TextBox.Tag
							for (int i=0; i < pnlPrompts.Controls.Count; i++)
							{
								if (	pnlPrompts.Controls[i].GetType().ToString().EndsWith("TextBox")
									&&	pnlPrompts.Controls[i].Tag.ToString() == strKey)
								{
									// Found a match, set Text = strVal
									TextBox t = (TextBox) pnlPrompts.Controls[i];
									t.Text = strVal;
								}
							}
						}
					}
					
					#endregion
					
				}
			}
			catch (Exception ex)
			{
				Logger.Log(LogLevel.Debug,"SetLineItem.PostPrompts","Error Posting Prompts " + ex.ToString() + " . " );
			}
		}
		#endregion
		
		#region private string GetReferencePath(TreeNode node)
		private string GetReferencePath(TreeNode node)
		{
			string path = node.FullPath;
			path = Regex.Replace ( path, @"....\[.*\]", "" );
			path = path.Replace ( "\\", "/" );
			path = path.Replace ( " - ", "/" );
			if (path.Substring(0,1)!="/")
			{
				path = "/" + path ;
			}
			
			return path;
		}
		#endregion
		
		#region private void tvwForms_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
		private void tvwForms_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			TreeNode node = e.Node;
			
			pnlPrompts.Controls.Clear();
			if (node.Nodes.Count > 0 )
			{
				if (node.FirstNode.ImageIndex == (int)IMAGE.PROMPT)
				{
					//Show the prompts and allow to save if all prompts are entered
					PostPrompts( node.FirstNode );
					
					txtGuide.Text						= "Please fill in prompts to continue.";
					txtReferenceNumber.Text				= this.GetReferencePath( node );
					cmdCustom.Enabled					= true;
					cmdSave.Enabled						= true;
					pnlImgLights.BackgroundImage		= Image.FromFile( Application.StartupPath + "\\Images\\yellow.gif" );
					
					// If there are prompts and Line items at the same level they need to choose a line item
					if (node.Nodes[node.Nodes.Count-1].ImageIndex != (int) IMAGE.PROMPT)
//					if (node.Nodes[0].ImageIndex == (int)IMAGE.PROMPT)
					{
						txtGuide.Text					= "Please continue selecting the reference point and fill in prompts to continue.";
						txtReferenceNumber.Text			= this.GetReferencePath( node );
						cmdCustom.Enabled				= false;
						cmdSave.Enabled					= false;
					//	pnlImgLights.BackgroundImage	= Image.FromFile( Application.StartupPath + "\\Images\\red.gif" );	
					}
				}
				else
				{
					txtGuide.Text						= "Ambiguous, please continue selecting the reference point.";
					txtReferenceNumber.Text				= this.GetReferencePath( node );
					cmdCustom.Enabled					= false;
					cmdSave.Enabled						= false;
					pnlImgLights.BackgroundImage		= Image.FromFile( Application.StartupPath + "\\Images\\red.gif" );
				}
			}
			else if ( node.ImageIndex == (int)IMAGE.PROMPT )
			{
				//Show all prompts for Children of the Parent of the Prompt
				txtGuide.Text							= "Required data-entry for nodes at this level; ambiguous, please continue selecting the reference point.";
				txtReferenceNumber.Text					= this.GetReferencePath( node );
				cmdCustom.Enabled						= false;
				cmdSave.Enabled							= false;
				pnlImgLights.BackgroundImage			= Image.FromFile( Application.StartupPath + "\\Images\\red.gif" );
			}
			else
			{
				//Clear the prompts
				pnlPrompts.Controls.Clear();
				
				// Go to first node under current parent
				// Prompts will always be at the beginning
				if ( node.Parent.FirstNode != null && node.Parent.FirstNode.ImageIndex == (int) IMAGE.PROMPT)
				{
					PostPrompts( node.Parent.FirstNode );
				}
				
				cmdCustom.Enabled				= true;
				cmdSave.Enabled					= true;
				txtGuide.Text					= "Valid reference point.";
				txtReferenceNumber.Text			= this.GetReferencePath( node );
				pnlImgLights.BackgroundImage	= Image.FromFile( Application.StartupPath + "\\Images\\green.gif" );
			}
		}
		#endregion
		
		#region private void ValidationEvent(object sender, ValidationEventArgs args)
		private void ValidationEvent(object sender, ValidationEventArgs args)
		{
			MessageBox.Show(this,"Error Loading Tax Form Reference Data.  Please contact your System Administrator.", "SetLineItem", MessageBoxButtons.OK, MessageBoxIcon.Error );
			Logger.Log(LogLevel.Error,"SetLineItem.ValidationEvent","Error Loading Tax Form Reference Data for year (" + mintExaminationYear.ToString() + ") " + args.Exception.ToString());
			//			Console.WriteLine("Xml validation failed. \nType: " + args.Severity +" \nMessage: " + args.Message);
			//			Console.ReadLine();
		}
		#endregion
		
		#region private void cmdSave_Click(object sender, System.EventArgs e)
		private void cmdSave_Click(object sender, System.EventArgs e)
		{
			//Make sure case is not closed or suspended. --Sszathmary 11/4/2004 (MJennings)
			if(!ICI_Functions.IsCaseModificationPermitted(this,m_case_pkey,this.Name,true))
			{
				return;
			}

			string strPrompts = "";
			if (pnlPrompts.Controls.Count > 0)
			{
				foreach (Control ltbx in pnlPrompts.Controls)
				{
					if (ltbx.GetType().ToString()!="System.Windows.Forms.TextBox") { continue; }
					
					if (((TextBox)ltbx).Text.Trim()=="")
					{
						MessageBox.Show(this,"All prompts Fields must be populated.","Set Line Item",MessageBoxButtons.OK,MessageBoxIcon.Information);
						ltbx.Focus();
						return;
					}
					else
					{
						strPrompts = strPrompts + "/|" + ltbx.Tag.ToString() + "," + ltbx.Text + "|";
					}
				}
			}
			// Need to determine if it is prefix or postfix to the last element in the ReferencePoint.
			if (tvwForms.SelectedNode.Nodes.Count > 0)
			{
				// Prompts are appended to the end
				txtReferenceNumber.Text = txtReferenceNumber.Text + strPrompts;
			}
			else
			{
				// Prompts are appended before the last element
				int lholder = txtReferenceNumber.Text.LastIndexOf("/");
				
				if (lholder >= 0)
				{
					string strstart			= txtReferenceNumber.Text.Substring(0, lholder);
					string strEnd			= txtReferenceNumber.Text.Substring(lholder);
					txtReferenceNumber.Text = strstart + strPrompts + strEnd;
				}
			}
			
			// Write the TaxLine (ReferenceNumber) and CustomWorksheet data to the 5701 xml doc
			try
			{
				Logger.Log(LogLevel.Debug,"SetLineItem.cmdCustom_Click","Saving ReferenceNumber (TaxLine) and CustomWorksheet data to 5701 Infopath xml document. " );
				
				string str5701Path = Application.StartupPath + "\\Cases\\" + m_case_pkey.ToString() + "\\Issues\\" + m_doc_guid + ".xml";

				// MJennings - 11/3/2004 - Issue 19267: remove the front portion of the ReferenceNumber (i.e. everything up to "/Line")
				string strReferenceNumber = txtReferenceNumber.Text;

				// MJennings - 11/8/2004 - Issue 19374: write the full ReferenceNumber to the xml doc, and handle the formatting within the InfoPath form
				// strReferenceNumber = strReferenceNumber.Remove(0, strReferenceNumber.IndexOf("/Line") + 1);

				ICI_Functions.SetIssueDocumentXMLData(str5701Path, "my:ReferenceNumber", strReferenceNumber);
				ICI_Functions.SetIssueDocumentXMLData(str5701Path, "my:CustomWorksheet", txtCustomWorksheet.Text);
			}
			catch (System.Exception ex)
			{
				Logger.Log(LogLevel.Debug,"SetLineItem.cmdCustom_Click","Error saving data to 5701 Infopath xml document " + ex.ToString() + " . " );
			}
			
			if (m_doc_guid != null)
			{
				try
				{
					object[] spParams;
					
					// Save TaxLine to IMS_DOCUMENT_DEMOGRAPHIC
					spParams = new object[6];
					spParams.SetValue(m_doc_guid,				0);
					spParams.SetValue("TaxLine",				1);
					spParams.SetValue(null,						2);
					spParams.SetValue(txtReferenceNumber.Text,	3);
					spParams.SetValue(null,						4);
					spParams.SetValue(null,						5);
					
					SqlHelper.ExecuteNonQuery(ICI_Functions.GetConnectionString(), "PSP_Save_IMS_Document_Demographic", spParams);
					
					// Save CustomWorksheet to IMS_DOCUMENT_DEMOGRAPHIC
					spParams = new object[6];
					spParams.SetValue(m_doc_guid,				0);
					spParams.SetValue("CustomWorksheet",		1);
					spParams.SetValue(null,						2);
					spParams.SetValue(txtCustomWorksheet.Text,	3);
					spParams.SetValue(null,						4);
					spParams.SetValue(null,						5);
					
					SqlHelper.ExecuteNonQuery(ICI_Functions.GetConnectionString(), "PSP_Save_IMS_Document_Demographic", spParams);
				}
				catch (Exception ex)
				{
				//	MessageBox.Show(this, "Error saving Custom Worksheet to database", "Custom Worksheet");
					Logger.Log(LogLevel.Debug,"SetLineItem.cmdCustom_Click","Error Saving TaxLine/CustomWorksheet " + ex.ToString() + " . " );
				}
			}
			
			this.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.Close();
		}
		#endregion
		
		#region private void cmdCancel_Click(object sender, System.EventArgs e)
		private void cmdCancel_Click(object sender, System.EventArgs e)
		{
			this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Close();
		}
		#endregion
		
		#region private void cmdCustom_Click(object sender, System.EventArgs e)
		private void cmdCustom_Click(object sender, System.EventArgs e)
		{
			//Make sure case is not closed or suspended. --Sszathmary 11/4/2004 (MJennings)
			if(!ICI_Functions.IsCaseModificationPermitted(this,m_case_pkey,this.Name,true))
			{
				return;
			}

			DialogResult	rc			= new DialogResult();
			string			strCustom	= "";
			Form			f			= new Forms.InputBox("Custom Worksheet", "Please enter worksheet data.");
							rc			= f.ShowDialog();
			
			if (rc == System.Windows.Forms.DialogResult.Cancel) { return; }
			
			TextBox txtCustom = ((Forms.InputBox)f).txtText;
			txtCustom.MaxLength = 255;

			strCustom = txtCustom.Text;
			while (MessageBox.Show(this,"Do you have more Custom Worksheet Data to Enter?","Custom Worksheet",MessageBoxButtons.YesNo,MessageBoxIcon.Question) == DialogResult.Yes)
			{
				rc = f.ShowDialog();
				if (rc == System.Windows.Forms.DialogResult.OK)
				{
					txtCustom = ((Forms.InputBox)f).txtText;
					txtCustom.MaxLength = 255;
					
					strCustom = strCustom  + "/" + txtCustom.Text;
				}
			}
			txtCustomWorksheet.Text = strCustom;
		}
		#endregion
		
		
		
		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(SetLineItem));
			this.cmdCustom = new System.Windows.Forms.Button();
			this.cmdCancel = new System.Windows.Forms.Button();
			this.cmdSave = new System.Windows.Forms.Button();
			this.lbllabel3 = new System.Windows.Forms.Label();
			this.lbllabel2 = new System.Windows.Forms.Label();
			this.lbllabel1 = new System.Windows.Forms.Label();
			this.txtReferenceNumber = new System.Windows.Forms.TextBox();
			this.txtGuide = new System.Windows.Forms.TextBox();
			this.tvwForms = new System.Windows.Forms.TreeView();
			this.pnlPrompts = new System.Windows.Forms.Panel();
			this.label1 = new System.Windows.Forms.Label();
			this.txtCustomWorksheet = new System.Windows.Forms.TextBox();
			this.pnlImgLights = new System.Windows.Forms.Panel();
			this.SuspendLayout();
			// 
			// cmdCustom
			// 
			this.cmdCustom.Location = new System.Drawing.Point(448, 368);
			this.cmdCustom.Name = "cmdCustom";
			this.cmdCustom.Size = new System.Drawing.Size(140, 23);
			this.cmdCustom.TabIndex = 150;
			this.cmdCustom.Text = "Custom Worksheet";
			this.cmdCustom.Click += new System.EventHandler(this.cmdCustom_Click);
			// 
			// cmdCancel
			// 
			this.cmdCancel.Location = new System.Drawing.Point(688, 368);
			this.cmdCancel.Name = "cmdCancel";
			this.cmdCancel.TabIndex = 170;
			this.cmdCancel.Text = "Cancel";
			this.cmdCancel.Click += new System.EventHandler(this.cmdCancel_Click);
			// 
			// cmdSave
			// 
			this.cmdSave.Location = new System.Drawing.Point(600, 368);
			this.cmdSave.Name = "cmdSave";
			this.cmdSave.TabIndex = 160;
			this.cmdSave.Text = "Save";
			this.cmdSave.Click += new System.EventHandler(this.cmdSave_Click);
			// 
			// lbllabel3
			// 
			this.lbllabel3.Location = new System.Drawing.Point(8, 24);
			this.lbllabel3.Name = "lbllabel3";
			this.lbllabel3.Size = new System.Drawing.Size(100, 16);
			this.lbllabel3.TabIndex = 16;
			this.lbllabel3.Text = "Path Builder";
			// 
			// lbllabel2
			// 
			this.lbllabel2.Location = new System.Drawing.Point(360, 112);
			this.lbllabel2.Name = "lbllabel2";
			this.lbllabel2.Size = new System.Drawing.Size(100, 13);
			this.lbllabel2.TabIndex = 15;
			this.lbllabel2.Text = "Path";
			// 
			// lbllabel1
			// 
			this.lbllabel1.Location = new System.Drawing.Point(360, 24);
			this.lbllabel1.Name = "lbllabel1";
			this.lbllabel1.Size = new System.Drawing.Size(100, 15);
			this.lbllabel1.TabIndex = 14;
			this.lbllabel1.Text = "Guide";
			// 
			// txtReferenceNumber
			// 
			this.txtReferenceNumber.Location = new System.Drawing.Point(352, 128);
			this.txtReferenceNumber.Multiline = true;
			this.txtReferenceNumber.Name = "txtReferenceNumber";
			this.txtReferenceNumber.Size = new System.Drawing.Size(427, 24);
			this.txtReferenceNumber.TabIndex = 3;
			this.txtReferenceNumber.Text = "";
			// 
			// txtGuide
			// 
			this.txtGuide.Location = new System.Drawing.Point(352, 40);
			this.txtGuide.Multiline = true;
			this.txtGuide.Name = "txtGuide";
			this.txtGuide.Size = new System.Drawing.Size(428, 64);
			this.txtGuide.TabIndex = 2;
			this.txtGuide.Text = "";
			// 
			// tvwForms
			// 
			this.tvwForms.ImageIndex = -1;
			this.tvwForms.Location = new System.Drawing.Point(8, 40);
			this.tvwForms.Name = "tvwForms";
			this.tvwForms.SelectedImageIndex = -1;
			this.tvwForms.Size = new System.Drawing.Size(328, 360);
			this.tvwForms.TabIndex = 1;
			this.tvwForms.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvwForms_AfterSelect);
			// 
			// pnlPrompts
			// 
			this.pnlPrompts.AutoScroll = true;
			this.pnlPrompts.Location = new System.Drawing.Point(352, 208);
			this.pnlPrompts.Name = "pnlPrompts";
			this.pnlPrompts.Size = new System.Drawing.Size(427, 152);
			this.pnlPrompts.TabIndex = 5;
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(360, 160);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(100, 13);
			this.label1.TabIndex = 22;
			this.label1.Text = "Custom Worksheet:";
			// 
			// txtCustomWorksheet
			// 
			this.txtCustomWorksheet.Location = new System.Drawing.Point(352, 176);
			this.txtCustomWorksheet.Multiline = true;
			this.txtCustomWorksheet.Name = "txtCustomWorksheet";
			this.txtCustomWorksheet.Size = new System.Drawing.Size(427, 24);
			this.txtCustomWorksheet.TabIndex = 4;
			this.txtCustomWorksheet.Text = "";
			// 
			// pnlImgLights
			// 
			this.pnlImgLights.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pnlImgLights.BackgroundImage")));
			this.pnlImgLights.Location = new System.Drawing.Point(784, 40);
			this.pnlImgLights.Name = "pnlImgLights";
			this.pnlImgLights.Size = new System.Drawing.Size(29, 60);
			this.pnlImgLights.TabIndex = 23;
			// 
			// SetLineItem
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(840, 418);
			this.Controls.Add(this.pnlImgLights);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.txtCustomWorksheet);
			this.Controls.Add(this.txtReferenceNumber);
			this.Controls.Add(this.txtGuide);
			this.Controls.Add(this.pnlPrompts);
			this.Controls.Add(this.cmdCustom);
			this.Controls.Add(this.cmdCancel);
			this.Controls.Add(this.cmdSave);
			this.Controls.Add(this.lbllabel3);
			this.Controls.Add(this.lbllabel2);
			this.Controls.Add(this.lbllabel1);
			this.Controls.Add(this.tvwForms);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "SetLineItem";
			this.Text = "Choose the Tax Form Line Item that the 5701 Applies to.";
			this.ResumeLayout(false);

		}
		#endregion
	}
}
