# quotes
A Windows application to show a quote of the day.

# Target Platform
Windows 11

# Technologies Used
WPF C# .NET 10

# Features
Application displays a quote of the day once a day, the first time I start or awaken my computer.
The quotes are stored as text files in a folder that can be configured.
The application will randomly select a quote from the folder and display it in a window.
The quote text is selectable and can be copied to the clipboard.
The application runs in the system tray.

The application shows a tray icon in the system tray, and when clicked, it will show the quote of the day in a window.

# Starting the app
Compile a release version. Then, you can start it from a PowerShell command prompt:
`Start-Process .\Quotes\bin\Release\net10.0-windows\Quotes.exe`
or you can run a debug version:
`Start-Process .\Quotes\bin\Debug\net10.0-windows\Quotes.exe`
