<?php
	set_error_handler('ErrorHandle');

	//	Establish constants
	const db_HOST = '';
	const db_NAME = '';
	const db_USER = '';
	const db_PASS = '';
	const APP_KEY = array('');
	const COOKIE = '';

	//	Verify authentication
	if (!isset($_COOKIE[COOKIE]) && !isset($_POST['editor'])) o(null,'No credentials available.');
	if (!isset($_POST['appKey'])) o(null,'Missing app key.');
	$cred = preg_replace('/[^\w]/','',strtoupper(((!isset($_COOKIE[COOKIE]))? $_POST['editor']:$_COOKIE[COOKIE])));
	$valid = false;
	foreach (APP_KEYS as $key => $value) $valid = $valid || (hash('sha256',$value.$cred) == $_POST['appKey']);
	if (!$valid) o(null,'Invalid api key presented.');

	//	Check necessary components
	if (!isset($_POST['table'])) o(null,'Missing table.');
	$table = $_POST['table'];
	$payload = (isset($_POST['payload']))? $_POST['payload'] : "";
	$isSave = $payload != "";
	$overwrite = isset($_POST['overwrite']);
	if (!isset($_POST['saveKey'])) o(null,'Missing saveKey.');
	$saveKey = ($_POST['saveKey'] != "*") ? preg_replace('/[^\w]/','',strtoupper($_POST['saveKey'])) : "*";

	$mysqli = new mysqli(db_HOST, db_USER, db_PASS, db_NAME);
	if ($mysqli->connect_errno) o(null, $mysqli->connect_error);
	if ($isSave)
	{
		$payload = (json_decode($payload) != null) ? json_encode((object)array_merge((array)json_decode($payload),array('timestamp'=>date(DATE_RFC3339)))) : $payload;
		if (!$mysqli->query("CREATE TABLE IF NOT EXISTS $table (saveKey VARCHAR(1023) PRIMARY KEY, saveVal TEXT);")) o(null, $mysqli->error);
		$result = (!$overwrite)? $mysqli->query("SELECT * FROM $table") : $mysqli->query("SELECT * FROM $table WHERE saveKey='$auth'");
		$q = ($result->num_rows == 0 || !$overwrite) ? 
			$mysqli->prepare("INSERT INTO $table (saveKey, saveVal) VALUES('".(($overwrite)? $auth : $result->num_rows)."', ?)") :
			$mysqli->prepare("UPDATE $table SET saveVal=? WHERE saveKey='$auth'");
		$q->bind_param('s', $payload);
		if (!$q->execute()) o(null, $mysqli->error);
		o(date(DATE_RFC3339), null);
	}
	else
	{
		$result = ($saveKey != "*") ? $mysqli->query("SELECT * FROM $table WHERE saveKey='$saveKey'") : $mysqli->query("SELECT * FROM $table");
		if ($result == null || $result->num_rows == 0) o(null, 'No data found.');
		if ($overwrite) o($result->fetch_assoc()['saveVal'], null);
		else 
		{
			$out = array();
			while($row = $result->fetch_assoc()) array_push($out, $row['saveVal']);
			o(json_encode($out), null);
		}
	}
	function o($v,$e){die(json_encode((object)array('value'=>$v,'error'=>$e)));}
	///	Used to handle any errors
	function ErrorHandle($errNo, $errStr, $errFile, $errLine) {
	    $msg = "$errStr in $errFile on line $errLine";
	    o(null, $msg);
	}
?>