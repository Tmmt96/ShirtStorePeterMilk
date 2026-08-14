# User experience tests

The Playwright suite covers desktop and mobile routes, image loading, the mobile
menu, cart quantity updates, checkout validation, and stops before payment.

Install dependencies once:

```powershell
npm install
```

Run against the local site:

```powershell
$env:UX_BASE_URL = "https://localhost:44300"
$env:PLAYWRIGHT_EXECUTABLE_PATH = "C:\Program Files\Google\Chrome\Application\chrome.exe"
npm test
```

The application must already be running and the Umbraco installation must be
complete before starting the suite.