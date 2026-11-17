const { test, expect } = require('@playwright/test');

const pages = [
  '/',
  '/about.html',
  '/campaigns.html',
  '/campaign-details.html',
  '/login.html',
  '/signup.html',
  '/contributor/dashboard.html',
  '/organizer/dashboard.html',
  '/public/institutional-ledger.html',
  '/treasurer/dashboard.html',
  '/treasurer/ledger.html'
];

// TEST 1: Check all 11 pages
for (const path of pages) {
  test(`Page loads: ${path}`, async ({ page }) => {
    const response = await page.goto(path);

    expect(response.status()).toBe(200);
    await expect(page.locator('body')).toBeVisible();
  });
}

// TEST 2: Check images
for (const path of pages) {
  test(`Images load: ${path}`, async ({ page }) => {
    await page.goto(path);

    const brokenImages = await page.locator('img').evaluateAll(images =>
      images
        .filter(img => !img.complete || img.naturalWidth === 0)
        .map(img => img.src)
    );

    expect(brokenImages).toEqual([]);
  });
}

// TEST 3: Check mobile layout
for (const path of pages) {
  test(`Mobile layout: ${path}`, async ({ page }) => {
    await page.setViewportSize({
      width: 375,
      height: 812
    });

    await page.goto(path);

    const dimensions = await page.evaluate(() => ({
      screen: document.documentElement.clientWidth,
      content: document.documentElement.scrollWidth
    }));

    expect(dimensions.content).toBeLessThanOrEqual(
      dimensions.screen
    );
  });
}const { test, expect } = require('@playwright/test');

const pages = [
  '/',
  '/about.html',
  '/campaigns.html',
  '/campaign-details.html',
  '/login.html',
  '/signup.html',
  '/contributor/dashboard.html',
  '/organizer/dashboard.html',
  '/public/institutional-ledger.html',
  '/treasurer/dashboard.html',
  '/treasurer/ledger.html'
];

// TEST 1: Check all 11 pages
for (const path of pages) {
  test(`Page loads: ${path}`, async ({ page }) => {
    const response = await page.goto(path);

    expect(response.status()).toBe(200);
    await expect(page.locator('body')).toBeVisible();
  });
}

// TEST 2: Check images
for (const path of pages) {
  test(`Images load: ${path}`, async ({ page }) => {
    await page.goto(path);

    const brokenImages = await page.locator('img').evaluateAll(images =>
      images
        .filter(img => !img.complete || img.naturalWidth === 0)
        .map(img => img.src)
    );

    expect(brokenImages).toEqual([]);
  });
}

// TEST 3: Check mobile layout
for (const path of pages) {
  test(`Mobile layout: ${path}`, async ({ page }) => {
    await page.setViewportSize({
      width: 375,
      height: 812
    });

    await page.goto(path);

    const dimensions = await page.evaluate(() => ({
      screen: document.documentElement.clientWidth,
      content: document.documentElement.scrollWidth
    }));

    expect(dimensions.content).toBeLessThanOrEqual(
      dimensions.screen
    );
  });
}