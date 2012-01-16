<?php
/**
 * @file
 * Class to handle ISGweb SOAP requests and responses.
 */

class ISGwebWrapper {
  private $wsdl_root;
  private $wsdl_keys;
  private $ui_root;
  private $cache_dir;
  public  $errors;

  public function __construct(
  $wsdl_root,
  $wsdl_keys = array(
            'Authentication' => NULL,
            'DataAccess' => NULL
  ),
  $ui_root
  ) {
    $this->wsdl_root  = trim($wsdl_root, '/') . '/';
    $this->wsdl_keys  = $wsdl_keys;
    $this->ui_root    = trim($ui_root, '/') . '/';
    $this->cache_dir  = realpath(file_directory_path());
    $this->errors    = array();
  }


  /**
   * Authentication web service methods
   */
  public function AuthenticateUser($username, $password) {
    $response = $this->call(
            'Authentication', 'AuthenticateUser',
    array(
              'username'  => $username,
              'password'  => $password
    ));

    if ($response) {
      $response = $response['User']['@attributes'];
      $response = $this->AuthenticateToken($response['TOKEN']); // Authorize the issued token in ISGweb
    }

    return $response;
  }


  public function AuthenticateToken($token) {
    $response = $this->call(
            'Authentication', 'AuthenticateToken',
    array(
              'token'  => $token
    ));

    if (!empty($response)) {
      $response = $response['User']['@attributes'];
    }

    return $response;
  }


  public function DeleteUserSession($token) {
    $response = $this->call(
            'Authentication', 'DeleteUserSession',
    array(
              'token'  => $token
    ));

    if (!empty($response)) {
      $response = TRUE;
    }

    return $response;
  }



  /**
   * DataAccess web service methods
   */
  public function ExecuteStoredProcedure($sproc, $args) {
    $response = $this->call(
            'DataAccess', 'ExecuteStoredProcedure',
    array(
              'name'      => $sproc,
              'parameters'  => $args
    ));

    return $response;
  }



  /**
   * UI helper methods
   */
  public function GetLink($link_key, $token, $returnURL, $is_iframe = FALSE) {
    $links = array(
      'profile_view'     => 'Profile/ViewProfile.aspx',
      'profile_edit'     => 'Profile/EditProfile.aspx',
      'profile_register'  => 'Profile/CreateNewUser.aspx',
      'profile_password'  => 'LogIn/RetrievePassword.aspx',
      'iframe_js'      => 'javascripts/iframe.js',
    );

    if (array_key_exists($link_key, $links)) {
      $format  = $this->ui_root . $links[$link_key];
      $query  =
      $params  = array();

      if (!empty($token)) {
        $query[]  = 'Token=%s';
        $params[]  = $token;
      }
      if (!empty($returnURL))  {
        $query[]   = 'ReturnPage=%s';
        $params[]  = $returnURL;
      }
      if (!empty($query)) {
        $format .= '?'. implode('&', $query);
      }

      $path = vsprintf($format, $params);

      if ($is_iframe) {
        drupal_set_html_head(
          '<script type="text/javascript" src="' . $this->ui_root . $links['iframe_js'] . '"></script>'
          );

          return '<iframe src="' . $path . '" isgwebsite="1" name="ISGwebContainer" id="ISGwebContainer" marginwidth="1" marginheight="0" frameborder="0" vspace="0" hspace="0" scrolling="no" style="width:100%; height:800px;"></iframe>';
      }

      return $path;
    }

    return NULL;
  }



  /**
   * Internal utility methods
   */
  private function call($service, $method, $params) {
    ini_set('memory_limit', '1024M');
    ini_set('max_execution_time', 1800);
    set_time_limit(0);

    $url    = $this->wsdl_root . $service . '.asmx?wsdl';
    $timeout  = 3000;
    $cache    = new nusoap_wsdlcache($this->cache_dir , $timeout);
    $wsdl    = $cache->get($url);

    // Set the WSDL
    if (is_null($wsdl)) {
      $wsdl  = new wsdl($url, NULL, NULL, NULL, NULL, 0, $timeout, NULL, TRUE);
      $error  = $wsdl->getError();
      $debug  = $wsdl->getDebug();
      $wsdl->clearDebug();

      // Check for SOAP errors
      if (!empty($error)) {
        $this->errors[] = $error;
        if ($debug) {
          $this->errors[] = '<pre>' . print_r($debug, TRUE) . '</pre>';
        }

        return FALSE;
      }

      $cache->put($wsdl);
    }


    // Send the SOAP request
    $params['securityPassword']  = $this->wsdl_keys[$service];
    $client            = new nusoap_client($wsdl, 'wsdl', FALSE, FALSE, FALSE, FALSE, 0, $timeout);
    $client->setDebugLevel(0); // 0 - 9, where 0 is off
    $client->useHTTPPersistentConnection();

    if ($service == 'DataAccess' && $method == 'ExecuteStoredProcedure') {
      /*
       * See http://www.codingforums.com/archive/index.php/t-85260.html
       * and http://users.skynet.be/pascalbotte/rcx-ws-doc/nusoapadvanced.htm
       * for how to thwart the "got wsdl error: phpType is struct, but value is not an array"
       * error returned by nusoap when processing the response from $client->call()
       *
       * */
      $request = $client->serializeEnvelope(
      vsprintf('<ExecuteStoredProcedure xmlns="http://ibridge.isgsolutions.com/%s/">
                      <securityPassword>%s</securityPassword>
                      <name>%s</name>
                      <parameters>%s</parameters>
                      </ExecuteStoredProcedure>',
      array(
      $service,
      $params['securityPassword'],
      $params['name'],
      $params['parameters']
      )));

      $response = $client->send($request, 'http://ibridge.isgsolutions.com/' . $service . '/' . $method, 0, $timeout);
    }
    else {
      $response = $client->call($method, $params);
    }


    $error  = $client->getError();
    $debug  = $client->getDebug();
    $client->clearDebug();

    // Check for SOAP errors
    if (!empty($error)) {
      $this->errors[] = $error;
      if ($debug) {
        $this->errors[] = '<pre>' . print_r($debug, TRUE) . '</pre>';
      }

      return FALSE;
    }


    // Process response
    $response  = $response[$method . 'Result'];
    $data    = NULL;

    if (strpos($response, '<') == 0) { // Some ISGweb methods return strings instead of XML

      libxml_use_internal_errors(TRUE);

      $response  = preg_replace('/(<\?xml[^?]+?)utf-16/i', '$1utf-8', $response); // Change encoding string to UTF8
      $response  = utf8_encode($response);
      $response  = $this->strip_invalid_xml($response);
      $obj    = simplexml_load_string($response);
      $data    = $response;
      $error    = libxml_get_errors();

      // Check for XML parsing errors
      if (!empty($error)) {
        foreach ($error as $e) {
          $this->errors[] = $e;
        }
        libxml_clear_errors();

        return FALSE;
      }

      $data = $this->object_to_array($obj);

      // Check for ISGweb errors (e.g. invalid data input, failure of service, etc.)
      if (array_key_exists('Errors', $data)) {
        $error = $data['Errors'];
        foreach ($error as $e) {
          $this->errors[] = $e['@attributes']['Description'];
        }

        return FALSE;
      }
    }
    else {
      $data = $response;
    }

    return $data;
  }


  private function object_to_array($arrObjData, $arrSkipIndices = array()) {
    $arrData = array();

    // if input is object, convert into array
    if (is_object($arrObjData)) {
      $arrObjData = get_object_vars($arrObjData);
    }

    if (is_array($arrObjData)) {
      foreach ($arrObjData as $index => $value) {
        if (is_object($value) || is_array($value)) {
          $value = $this->object_to_array($value, $arrSkipIndices); // recursive call
        }
        if (in_array($index, $arrSkipIndices)) {
          continue;
        }
        $arrData[$index] = $value;
      }
    }
    return $arrData;
  }


  private function strip_invalid_xml($value) {
    $ret = "";
    $current;
    if (empty($value)) {
      return $ret;
    }

    $length = strlen($value);
    for ($i=0; $i < $length; $i++) {
      $current = ord($value{$i});
      if (($current == 0x9) ||
      ($current == 0xA) ||
      ($current == 0xD) ||
      (($current >= 0x20) && ($current <= 0xD7FF)) ||
      (($current >= 0xE000) && ($current <= 0xFFFD)) ||
      (($current >= 0x10000) && ($current <= 0x10FFFF))
      ) {
        $ret .= chr($current);
      }
      else {
        $ret .= " ";
      }
    }

    return $ret;
  }


}