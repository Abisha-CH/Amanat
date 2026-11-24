const { defineConfig } = require('@playwright/test');

const port = 4173;
const baseURL = process.env.AMANAT_BASE_URL || `http://127.0.0.1:${port}`;

module.exports = defineConfig({
  testDir: './tests',
  fullyParallel: true,
  reporter: [['html', { open: 'never' }]],

  use: {
    baseURL,
    channel: 'chrome',
    headless: true,
    ignoreHTTPSErrors: process.env.AMANAT_IGNORE_HTTPS_ERRORS === 'true',
    viewport: { width: 1280, height: 800 },
    screenshot: 'only-on-failure',
    trace: 'on-first-retry'
  },

  webServer: process.env.AMANAT_BASE_URL ? undefined : {
    command: `dotnet run --urls ${baseURL}`,
    url: baseURL,
    reuseExistingServer: !process.env.CI,
    timeout: 120000,
    env: {
      ASPNETCORE_ENVIRONMENT: 'Development',
      ConnectionStrings__IdentityConnection: 'Data Source=C:/Users/abish/AppData/Local/Temp/opencode/playwright-spec.db'
    }
  }
});
