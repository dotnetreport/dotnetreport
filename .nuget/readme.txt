----------------------------------------
Thank you for installing dotnetreport.
----------------------------------------

The nuget package adds client side code to your project for dotnet report, https://www.dotnetreport.com. 

Installing this package adds app setting keys to your web.config, and you have to edit your web.config 
and replace the dummy values with your Account Api tokens. 

You can run your project and navigate to:
	http://localhost/dotnetreport for the Report Builder
	http://localhost/dotnetreport/dashboard for the Reports Dashboard
	http://localhost/dotnetsetup for the Admin Setup 

To start the Scheduler Job, add the following line to your Application_Start() method in Global.asax:

	JobScheduler.Start();


For more details and documentation, you can visit https://www.dotnetreport.com/kb. 

