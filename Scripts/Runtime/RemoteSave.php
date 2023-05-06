<?php
	set_error_handler('ErrorHandle');

	//	Establish constants
	const db_HOST = '';
	const db_NAME = '';
	const db_USER = '';
	const db_PASS = '';
	const APP_KEY = '';
	const COOKIE = '';

	//	Verify authentication
	if (!isset($_COOKIE[COOKIE]) && !isset($_POST['editor'])) o(null,'Unauthorized user.');
	if (!isset($_POST['auth'])) o(null,'Missing authentication.');
	if (!isset($_POST['appKey'])) o(null,'Missing key.');
	$cookie = preg_replace('/[^\w]/','',strtoupper(((!isset($_COOKIE[COOKIE]))? $_POST['editor']:$_COOKIE[COOKIE])));
	$auth = preg_replace('/[^\w]/','',strtoupper($_POST['auth']));
	if (hash('sha256',APP_KEY.$cookie) != $_POST['appKey']) o(null,'Invalid key presented.');

	//	Check necessary components
	if (!isset($_POST['table'])) o(null,'Missing table.');
	$table = $_POST['table'];
	$payload = (isset($_POST['payload']))? $_POST['payload'] : "";

	$mysqli = new mysqli(db_HOST, db_USER, db_PASS, db_NAME);
	if ($mysqli->connect_errno) o(null, $mysqli->connect_error);
	if ($payload != "")
	{
		$payload = (json_decode($payload) != null) ? json_encode((object)array_merge((array)json_decode($payload),array('timestamp'=>date(DATE_RFC3339)))) : $payload;
		if (!$mysqli->query("CREATE TABLE IF NOT EXISTS $table (saveKey VARCHAR(1023) PRIMARY KEY, saveVal TEXT);")) o(null, $mysqli->error);
		$result = $mysqli->query("SELECT * FROM $table WHERE saveKey='$auth'");
		$q = ($result->num_rows == 0) ? 
			$mysqli->prepare("INSERT INTO $table (saveKey, saveVal) VALUES('$auth', ?)") :
			$mysqli->prepare("UPDATE $table SET saveVal=? WHERE saveKey='$auth'");
		$q->bind_param('s', $payload);
		if (!$q->execute()) o(null, $mysqli->error);
		o(date(DATE_RFC3339), null);
	}
	else
	{
		$result = $mysqli->query("SELECT * FROM $table WHERE saveKey='$auth'");
		if ($result == null || $result->num_rows == 0) o(null, 'Key not found.');
		o(($result->fetch_assoc())['saveVal'], null);
	}
	function o($v,$e){die(json_encode((object)array('value'=>$v,'error'=>$e)));}
	///	Used to handle any errors
	function ErrorHandle($errNo, $errStr, $errFile, $errLine) {
	    $msg = "$errStr in $errFile on line $errLine";
	    o(null, $msg);
	}
?>