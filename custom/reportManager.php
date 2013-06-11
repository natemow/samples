
<!-- saved from url=(0110)https://interactive.springloops.io/project/16065/svn/source/raw/86/trunk%2Ccode%2Cmanagers%2CreportManager.php -->
<html><head><meta http-equiv="Content-Type" content="text/html; charset=ISO-8859-1"></head><body><pre style="word-wrap: break-word; white-space: pre-wrap;">&lt;?php
// Append server Zend Framework path + local AdWords lib
ini_set('include_path', ini_get('include_path').PATH_SEPARATOR.'C:\xampplite\php');
ini_set('include_path', ini_get('include_path').PATH_SEPARATOR.'C:\Projects\clients.interactivestrategies.com\trunk\lib\adwords');

// Append server Zend Framework path + local AdWords lib
// On Linux, make sure the vhost.conf's open_basedir value has the /usr/share path appended
ini_set('include_path', ini_get('include_path').PATH_SEPARATOR.'/usr/share');
ini_set('include_path', ini_get('include_path').PATH_SEPARATOR.'/var/www/vhosts/interactiverequest.com/subdomains/clients/httpdocs/lib/adwords');
ini_set('include_path', ini_get('include_path').PATH_SEPARATOR.'/var/www/vhosts/interactivestrategies.com/subdomains/clients/httpdocs/lib/adwords');

/*
 * Google Analytics wrapper class
 * 
 * */
	require_once('Zend/Gdata/ClientLogin.php');
	require_once('Zend/Gdata/Analytics.php');
	
	class ReporterGoogleAnalytics
	{
		private $service	= null;
		private $profile	= null;
		
		public function __construct($email, $pass, $webPropertyId)
		{
			$client			= Zend_Gdata_ClientLogin::getHttpClient($email, $pass, Zend_Gdata_Analytics::AUTH_SERVICE_NAME);
			$this-&gt;service	= new Zend_Gdata_Analytics($client);
			$account		= $this-&gt;getProfile($webPropertyId);
			$this-&gt;profile	= intval($account['profileId']);
		}
		
		
		private function getProfile($webPropertyId)
		{
			$rows	= $this-&gt;service-&gt;getAccountFeed();
			$fields	= array('webPropertyId', 'accountId', 'profileId', 'tableId', 'accountName', 'title');
			$data	= array();
			
			$ix = 0;
			foreach($rows as $row) {
				$data[$ix] = array();
				
				foreach($fields as $key) {
					$data[$ix][$key] = strval( $row-&gt;{$key} );
				}
				
				if ($data[$ix]['webPropertyId'] == $webPropertyId) {
					return $data[$ix];
				}
				
				$ix++;
			}
			
			return $data;
		}
		
		
		public function getData()
		{
			$query = $this-&gt;service-&gt;newDataQuery()
					    -&gt;setProfileId($this-&gt;profile)
					    -&gt;addDimension(Zend_Gdata_Analytics_DataQuery::DIMENSION_MEDIUM)
					    -&gt;addDimension(Zend_Gdata_Analytics_DataQuery::DIMENSION_SOURCE)
					    -&gt;addMetric(Zend_Gdata_Analytics_DataQuery::METRIC_BOUNCES)
					    -&gt;addMetric(Zend_Gdata_Analytics_DataQuery::METRIC_VISITS)
					    -&gt;setFilter(Zend_Gdata_Analytics_DataQuery::DIMENSION_MEDIUM."==referral")
					    -&gt;setStartDate('2008-10-01')
					    -&gt;setEndDate('2008-10-31')
					    -&gt;setSort(Zend_Gdata_Analytics_DataQuery::METRIC_VISITS, true)
					    -&gt;setMaxResults(25);
			
		    return $this-&gt;service-&gt;getDataFeed($query);
		}
		
				
	}
	
	
/*
 * Google AdWords wrapper class
 * 
 * */
	require_once('Google/Api/Ads/AdWords/Lib/AdWordsUser.php');
	require_once('Google/Api/Ads/AdWords/Util/ReportUtils.php');
	
	class ReporterGoogleAdWords extends dbManager
	{
		private $user;
		private $service;
		
		public function __construct($clientId = null)
		{
			parent::__construct();
			
			$user = new AdWordsUser();
			if (!empty($clientId)) { $user-&gt;SetClientId($clientId); }
			$user-&gt;LogDefaults(); // Log SOAP XML request and response.
			
			$this-&gt;user = $user;
		}
			
		
		public function getReports()
		{
			// Get the GetReportDefinitionService.
			$this-&gt;service = $this-&gt;user-&gt;GetReportDefinitionService('v201101');	
			
			try {
				  // Get all report definitions.
				$selector	= new ReportDefinitionSelector();
				$page		= $this-&gt;service-&gt;get($selector);
				$data		= array();
				
				// Display report definitions.
				if (isset($page-&gt;entries)) {
					foreach ($page-&gt;entries as $reportDefinition) {
						$data[] = array(
							'id'	=&gt; $reportDefinition-&gt;id,
							'name'	=&gt; $reportDefinition-&gt;reportName
						);
					}
				}
				
				return $data;
			}
			catch (Exception $e) {
				print $e-&gt;getMessage();
			}
		}
		
		
		public function syncData()
		{
			ini_set('memory_limit', '1024M');
			ini_set('max_execution_time', 1800);
			set_time_limit(0);
    		
			try
			{
				$this-&gt;service	= $this-&gt;user-&gt;GetServicedAccountService('v201101');
				$selector		= new ServicedAccountSelector();
				$obj			= $this-&gt;service-&gt;get($selector);
				
				if (isset($obj-&gt;accounts)) {
					for ($i=0; $i &lt; count($obj-&gt;accounts); $i++) {
						$acct = $obj-&gt;accounts[$i];
						
						if (empty($acct-&gt;canManageClients)) // Omit AdWords manager account
						{
							print $acct-&gt;customerId.'&lt;br /&gt;';
							print $acct-&gt;companyName.'&lt;br /&gt;';
							$this-&gt;syncAccountData($acct-&gt;customerId); 
						}
					}
				}
			}
			catch (Exception $e) 
			{
				print $e-&gt;getMessage();
			}
					
		}
		
		
		private function syncAccountData($customerId)
		{
			// Set current client
			$this-&gt;user-&gt;SetClientId($customerId);
			
			// Get the GetReportDefinitionService.
			$this-&gt;service = $this-&gt;user-&gt;GetReportDefinitionService('v201101');	
			
			// Set 5 year date range
			$dt_start	= (date('Y')-5).'0101';
			$dt_end		= date('Y').date('m').'01';
			$dt_range	= new DateRange($dt_start, $dt_end);
			
			$rpt_name	= 'All-time Campaign Performance Report: '. $dt_start.' - '.$dt_end;
			$rpt_defid	= null;
			
			// Check if this report already exists
			$reports = $this-&gt;getReports();
			for ($i=0; $i &lt; count($reports); $i++) {
				if ($rpt_name == $reports[$i]['name']) {
					$rpt_defid = $reports[$i]['id'];
				}
			}
			
			// Set field selector
			$rpt_selector				= new Selector();
			$rpt_selector-&gt;fields		= array('CampaignId', 'CampaignName', 'Month', 'Impressions', 'Clicks', 'Ctr', 'AverageCpc', 'AveragePosition', 'ConversionRate', 'Cost');
			$rpt_selector-&gt;dateRange	= $dt_range;
			
			// Set report definition
			$rpt_def					= new ReportDefinition();
			if (!empty($rpt_defid)) {
			$rpt_def-&gt;id				= $rpt_defid;
			}
			$rpt_def-&gt;reportName		= $rpt_name;
			$rpt_def-&gt;dateRangeType		= 'CUSTOM_DATE';
			$rpt_def-&gt;reportType		= 'CAMPAIGN_PERFORMANCE_REPORT';
			$rpt_def-&gt;downloadFormat	= 'XML';
			$rpt_def-&gt;selector			= $rpt_selector;			
			
			// Add or update report definition in AdWords
			$rpt_op						= new ReportDefinitionOperation();
			$rpt_op-&gt;operand			= $rpt_def;
			$rpt_op-&gt;operator			= (empty($rpt_defid) ? 'ADD' : 'SET');			
			$rpt_ops					= array($rpt_op);
			$rpt_result					= $this-&gt;service-&gt;mutate($rpt_ops);
			
			// Set local report definition id
			if ($rpt_result != null) {
				foreach ($rpt_result as $def) {
					$rpt_defid = $def-&gt;id;
				}
			}
			
			
			// Download report
			$filepath	= str_ireplace('/', DIRECTORY_SEPARATOR, dirname(__FILE__).'/../../reports/');
			$file		= $filepath.$rpt_defid.'.xml';
			ReportUtils::DownloadReport($rpt_defid, $file, $this-&gt;user);
			
			// Parse report and dump it all into the local db
			if (file_exists($file))
			{
				print $file.'&lt;br/&gt;&lt;br/&gt;';
				$xml	= simplexml_load_file($file);
				$parse	= $this-&gt;object_to_array($xml);
								
				if (array_key_exists('row', $parse['table']))
				{
					mysql_select_db(DB_DATABASE, parent::connect());
					
					for ($i=0; $i &lt; count($parse['table']['row']); $i++)
					{
						$row		= $parse['table']['row'][$i]['@attributes'];
						
						$sql_insert	= "INSERT INTO reporting_adwords (client_id,campaign_client_id,dt_updated,campaign_id,campaign_name,campaign_period,impressions,clicks,cost,leads,cpl,avg_position,cpc,ctr,conversion_rate,report_upload) VALUES (%d, '%s', '%s', '%s', '%s', '%s', %d, %d, %F, %d, %F, %F, %F, %F, %F, '');"; 
						$sql_insert	= vsprintf($sql_insert, array(
										null,
										$customerId,
										date('Y-m-d H:i:s'),
										mysql_real_escape_string($row['campaignID']),
										mysql_real_escape_string($row['campaign']),
										date('Y-m-d', strtotime($row['month'])),
										$row['impressions'],
										$row['clicks'],
										$row['cost'],
										0,
										0, // CPL = Cost/Leads
										$row['avgPosition'],
										$row['avgCPC'],
										str_ireplace('%', '', $row['ctr']),
										0 //str_ireplace('%', '', $row['convRate1PerClick'])
									));
									
						$sql_update	= "UPDATE reporting_adwords SET dt_updated = '%s', campaign_name = '%s', impressions = %d, clicks = %d, cost = %F, avg_position = %F, cpc = %F, ctr = %F WHERE campaign_client_id = '%s' AND campaign_id = '%s' AND campaign_period = '%s';";
						$sql_update	= vsprintf($sql_update, array(
										date('Y-m-d H:i:s'),
										mysql_real_escape_string($row['campaign']),
										$row['impressions'],
										$row['clicks'],
										$row['cost'],
										$row['avgPosition'],
										$row['avgCPC'],
										str_ireplace('%', '', $row['ctr']),
										//str_ireplace('%', '', $row['convRate1PerClick']),										
										$customerId,
										mysql_real_escape_string($row['campaignID']),
										date('Y-m-d', strtotime($row['month']))
									));
									
						$sql_select	= "SELECT COUNT(ad.campaign_id) AS `campaign_count` FROM reporting_adwords ad WHERE ad.campaign_client_id = '%s' AND ad.campaign_id = '%s' AND ad.campaign_period = '%s';";
						$sql_select	= vsprintf($sql_select, array(
										$customerId,
										mysql_real_escape_string($row['campaignID']),
										date('Y-m-d', strtotime($row['month']))
									));
						
						
						
						$result = mysql_query($sql_select);
						$count	= 0;
						
						if ($result) {
							while ($row = mysql_fetch_assoc($result)) {
								$count = $row['campaign_count'];
							}
							mysql_free_result($result);
							
							if (empty($count)) {
								$result = mysql_query($sql_insert);
							}
							else {
								$result = mysql_query($sql_update);
							}
							
							if (!$result) {
								$message  = 'Invalid query: ' . mysql_error() . "\n";
								$message .= 'Whole query: ' . $query;
								die($message);
							}
						}
						
						
					}
					
					parent::disconnect();
				}
				
			}
			
			
		}
		
		
		
		// Utility functions
		
		private function object_to_array($arrObjData, $arrSkipIndices = array())
		{
		    $arrData = array();
		   
		    // if input is object, convert into array
		    if (is_object($arrObjData)) {
		        $arrObjData = get_object_vars($arrObjData);
		    }
		   
		    if (is_array($arrObjData)) {
		        foreach ($arrObjData as $index =&gt; $value) {
		            if (is_object($value) || is_array($value)) {
		                $value = $this-&gt;object_to_array($value, $arrSkipIndices); // recursive call
		            }
		            if (in_array($index, $arrSkipIndices)) {
		                continue;
		            }
		            $arrData[$index] = $value;
		        }
		    }
		    return $arrData;
		}
		
		
	}
	
	
/*
 * Manager class
 * 
 * */
	require_once('code/dbManager.php');
	require_once('scripts/fileuploader/fileuploader.php');
	
	class reportManager extends dbManager
	{
		public function __construct()
		{
			parent::__construct();
		}
		
		
		public function adwords_sync()
		{
			$obj = new ReporterGoogleAdWords();
			$obj-&gt;syncData();
		}
		
		
		public function adwords_query($params)
		{	
			$sql = "SELECT	ad.client_id,ad.campaign_id,ad.campaign_period,ad.impressions,ad.clicks,ad.ctr,ad.cpc,ad.avg_position,ad.leads,ad.conversion_rate,ad.cost,ad.cpl,ad.report_upload 
					FROM	reporting_adwords AS ad 
					WHERE	ad.client_id = %d
					" . (!empty($params['campaign_id']) ? "
					AND		ad.campaign_id = '".$params['campaign_id']."'
					" : "") . "
					AND		ad.campaign_period &gt;= '%s'
					AND		ad.campaign_period &lt;= '%s'
					ORDER BY ad.campaign_period
					;";
			
			$sql	= vsprintf($sql, array(
							$params['client']-&gt;id,
							$params['dt_start'],
							$params['dt_end']
						));
			
			mysql_select_db(DB_DATABASE, parent::connect());
			
			$result = mysql_query($sql);
			$data	= array();
			
			if ($result) {
				while ($row = mysql_fetch_assoc($result)) {
					$data[] = $row;
				}
				mysql_free_result($result);
			}
			else {
				$message  = 'Invalid query: ' . mysql_error() . "\n";
				$message .= 'Whole query: ' . $sql;
				die($message);
			}
			
			parent::disconnect();
			
			return $data;
		}
		
		
		public function adwords_update($params)
		{
			$messages = 
			$sql_updates = array();
			
			if (!empty($params['leads'])) {
				if ($params['leads']==-1 || !is_numeric($params['leads'])) {
					$params['leads'] = 0;
				}
				
				$sql_updates[] = " leads = ".$params['leads']." ";
				$sql_updates[] = " cpl = " . (!empty($params['leads']) ? "(cost/".$params['leads'].")" : "0");
				$sql_updates[] = " conversion_rate = (".$params['leads']."/clicks)";
			}
			
			if (!empty($params['report_upload'])) {
				if ($params['report_upload']==1) {
					$ext		= array('doc','docx','xls','xlsx','txt','pdf');
					$size		= 10 * 1024 * 1024;
					$uploader	= new qqFileUploader($ext, $size);
					$result		= $uploader-&gt;handleUpload('reports/');
					$messages	= array_merge($messages, $result); 
					
					$sql_updates[] = " report_upload = '".$result['file']."' ";
				}
				elseif ($params['report_upload']==-1) {
					$sql_updates[] = " report_upload = '' ";
				}
			}
			
			if (!empty($params['campaign_period'])) {
				$params['campaign_period'] = date('Y-m-d 00:00:00', $params['campaign_period']);
			}
			
			$sql	= "	UPDATE	reporting_adwords SET " .
						implode(",", $sql_updates) . "
						WHERE	client_id	= %d " .
						(!empty($params['campaign_id']) ? "
						AND		campaign_id	= '".$params['campaign_id']."' "
						: "") . "
						AND		campaign_period	= '%s'
						;";
			
			$sql	= vsprintf($sql, array(
							$params['client_id'],
							$params['campaign_period']
						));
					
			mysql_select_db(DB_DATABASE, parent::connect());
			
			$result = mysql_query($sql);
			
			if (!$result) {
				$messages['mysql_error'] = mysql_error();
				$messages['mysql_query'] = $sql;
			}
			else {
				$client = new stdClass();
				$client-&gt;id = $params['client_id'];
				
				$params['client'] = $client;
				$params['dt_start'] = $params['campaign_period'];
				$params['dt_end'] = $params['campaign_period'];
				
				$data = $this-&gt;adwords_query($params);
				
				$messages['data'] = $data;
			}
			
			parent::disconnect();
			
			return htmlspecialchars(json_encode($messages), ENT_NOQUOTES);
		}
		
		
		public function adwords_date_range($client)
		{
			$sql	= "	SELECT MIN(ad.campaign_period) AS `min`, MAX(ad.campaign_period) AS `max` FROM reporting_adwords AS ad WHERE ad.client_id = %d;";
			$sql	= vsprintf($sql, array(
							$client-&gt;id
						));
						
			mysql_select_db(DB_DATABASE, parent::connect());
			
			$result = mysql_query($sql);
			$data	= array();
			
			if ($result) {
				while ($row = mysql_fetch_assoc($result)) {
					$data[] = $row;
				}
				mysql_free_result($result);
			}
			else {
				$message  = 'Invalid query: ' . mysql_error() . "\n";
				$message .= 'Whole query: ' . $sql;
				die($message);		
			}
			
			parent::disconnect();
			
			return $data;
		}
		
		
		public function adwords_campaigns($client)
		{
			$sql	= "	SELECT DISTINCT ad.campaign_id,ad.campaign_name FROM reporting_adwords AS ad WHERE ad.client_id = %d ORDER BY ad.campaign_name;";
			$sql	= vsprintf($sql, array(
							$client-&gt;id
						));
						
			mysql_select_db(DB_DATABASE, parent::connect());
			
			$result = mysql_query($sql);
			$data	= array();
			
			if ($result) {
				while ($row = mysql_fetch_assoc($result)) {
					$data[] = $row;
				}
				mysql_free_result($result);
			}
			else {
				$message  = 'Invalid query: ' . mysql_error() . "\n";
				$message .= 'Whole query: ' . $sql;
				die($message);		
			}
			
			parent::disconnect();
			
			return $data;
		}	
		
	}

</pre></body></html>