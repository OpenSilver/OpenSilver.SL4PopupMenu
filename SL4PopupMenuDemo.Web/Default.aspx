<%@ Page Language="C#" AutoEventWireup="true" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
	<title>SL4PopupMenuDemo</title>
	<style type="text/css">
	html, body {
		height: 100%;
		overflow: auto;
	}
	body {
		padding: 0;
		margin: 0;
	}
	#silverlightControlHost {
		height: 100%;
		text-align:center;
	}
	</style>
	<script type="text/javascript" src="Silverlight.js"></script>
	<script type="text/javascript">
		function onSilverlightError(sender, args) {
			var appSource = "";
			if (sender != null && sender != 0) {
			  appSource = sender.getHost().Source;
			}
			
			var errorType = args.ErrorType;
			var iErrorCode = args.ErrorCode;

			if (errorType == "ImageError" || errorType == "MediaError") {
			  return;
			}

			var errMsg = "Unhandled Error in Silverlight Application " +  appSource + "\n" ;

			errMsg += "Code: "+ iErrorCode + "    \n";
			errMsg += "Category: " + errorType + "       \n";
			errMsg += "Message: " + args.ErrorMessage + "     \n";

			if (errorType == "ParserError") {
				errMsg += "File: " + args.xamlFile + "     \n";
				errMsg += "Line: " + args.lineNumber + "     \n";
				errMsg += "Position: " + args.charPosition + "     \n";
			}
			else if (errorType == "RuntimeError") {           
				if (args.lineNumber != 0) {
					errMsg += "Line: " + args.lineNumber + "     \n";
					errMsg += "Position: " +  args.charPosition + "     \n";
				}
				errMsg += "MethodName: " + args.methodName + "     \n";
			}

			throw new Error(errMsg);
		}
	</script>
</head>
<body style="background-color: #7CB8D7;">
	<form action="https://www.paypal.com/cgi-bin/webscr" method="post" style="text-align: center; padding: 4px;">
		<object data="data:application/x-silverlight-2," type="application/x-silverlight-2" width="800px" height="485px" style="margin: 50px 50px 0px 50px">
		  <param name="source" value="ClientBin/SL4PopupMenuDemo.xap#/Demo1.xaml"/>
		  <param name="onError" value="onSilverlightError" />
		  <param name="background" value="#7CB8D0" />
		  <param name="minRuntimeVersion" value="4.0.50401.0" />
		  <param name="autoUpgrade" value="true" />
		  <a href="http://go.microsoft.com/fwlink/?LinkID=149156&v=4.0.50401.0" style="text-decoration:none">
			  <img src="http://go.microsoft.com/fwlink/?LinkId=161376" alt="Get Microsoft Silverlight" style="border-style:none"/>
		  </a>
		</object>
		<iframe id="_sl_historyFrame" style="visibility:hidden;height:0px;width:0px;border:0px"></iframe>
		
		<input type="hidden" name="cmd" value="_s-xclick" />
		<input type="hidden" name="hosted_button_id" value="ZBR7CN4VTWSFJ" />
		<div style="color:Blue;">
			If you found this project useful and want to support its<br />
			future development then feel free to donate any amount.
		</div>
		<input type="image" src="https://www.paypal.com/en_US/i/btn/btn_donate_LG.gif" alt="PayPal - The safer, easier way to pay online!" style="margin:20px"/>
		<img alt="" border="0" src="https://www.paypal.com/en_US/i/scr/pixel.gif" width="1" height="1" />
	</form>
</body>
</html>
