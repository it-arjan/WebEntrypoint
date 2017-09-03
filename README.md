# WebEntrypoint
To see how webentrypoint fits in the overall system, 
see https://messagequeuefrontend.azurewebsites.net/systemlayout

# Most interesting
/QueueManager2.cs
-async message processing code

-offers paralell & sequential executiong mode

-uses sempahores implementing a max load on webservices

-auto-switches to sequential mode when max load on a service is reached

/ServiceCall Folder

-O-O including WebServiceFactory

-async web service calls

/Controllers

-Implementing the web api to drop a message in the queue

/WebSockets

-holds the code for the (now secure) SocketServer + SocketClient

/Helpers/SettingsChecker

-Auto-checks if all settings are present in app.config


