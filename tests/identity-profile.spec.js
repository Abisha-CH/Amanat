const { test, expect } = require('@playwright/test');

const baseURL = process.env.AMANAT_BASE_URL || 'http://127.0.0.1:4173';
const publicPassword = 'Amanat@2026';
const seededUsers = {
  Organizer: {
    email: process.env.AMANAT_SEED_ORGANIZER_EMAIL || 'organizer.seed@amanat.test',
    password: process.env.AMANAT_SEED_ORGANIZER_PASSWORD || 'SeedOrganizer@2026',
    fullName: 'Seeded Organizer'
  },
  Treasurer: {
    email: process.env.AMANAT_SEED_TREASURER_EMAIL || 'treasurer.seed@amanat.test',
    password: process.env.AMANAT_SEED_TREASURER_PASSWORD || 'SeedTreasurer@2026',
    fullName: 'Seeded Treasurer'
  }
};

function unique(prefix) {
  return prefix + '-' + Date.now() + '-' + Math.random().toString(36).slice(2, 7);
}

async function stubExternal(page) {
  await page.route('**/*', route => {
    const url = route.request().url();
    if (url.startsWith(baseURL)) return route.continue();
    if (url.includes('/bootstrap@5.3.3/dist/css/bootstrap.min.css')) {
      return route.fulfill({ status: 200, contentType: 'text/css', body: '' });
    }
    if (url.includes('/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/javascript',
        body: `document.addEventListener('click', function (event) {
          var toggle = event.target.closest('[data-bs-toggle="dropdown"]');
          if (!toggle) return;
          var target = toggle.getAttribute('data-bs-target');
          var menu = target ? document.querySelector(target) : toggle.parentElement.querySelector('.dropdown-menu');
          if (menu) menu.classList.add('show');
        });`
      });
    }
    if (url.includes('bootstrap-icons') || url.includes('fonts.googleapis.com') || url.includes('fonts.gstatic.com')) {
      return route.fulfill({ status: 200, contentType: 'text/css', body: '' });
    }
    return route.abort();
  });
}

async function registerContributor(page, email) {
  await page.goto('/Identity/Account/Register');
  await page.fill('#fullName', 'Profile Contributor');
  await page.fill('#email', email);
  await page.fill('#phone', '03001234567');
  await page.fill('#password', publicPassword);
  await page.fill('#confirm-password', publicPassword);
  await page.check('input[name="Input.Role"][value="Contributor"]');
  await page.check('#terms');
  await page.click('button[type=submit]');
  await expect(page).toHaveURL(/\/Contributor\/Dashboard$/);
}

async function login(page, role) {
  await page.goto('/Identity/Account/Login');
  await page.fill('#email', seededUsers[role].email);
  await page.fill('#password', seededUsers[role].password);
  await page.click('button[type=submit]');
  await expect(page).toHaveURL(new RegExp(`/${role}/Dashboard$`));
}

async function postProfileForm(page, fields) {
  return page.evaluate(async options => {
    const token = document.querySelector('input[name="__RequestVerificationToken"]');
    const body = new URLSearchParams();
    Object.entries(options.fields).forEach(([name, value]) => body.append(name, value));
    if (token) body.append('__RequestVerificationToken', token.value);
    const response = await fetch(window.location.pathname, {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: body.toString(),
      credentials: 'same-origin'
    });
    return { status: response.status, text: await response.text() };
  }, { fields });
}

async function scriptState(page) {
  return page.evaluate(() => {
    const scripts = [...document.scripts];
    const jquery = scripts.filter(script => script.src.includes('jquery-3.7.1.min.js'));
    const index = name => scripts.findIndex(script => script.src.includes(name));
    return {
      jqueryCount: jquery.length,
      jqueryLocal: jquery.every(script => new URL(script.src).origin === window.location.origin),
      jqueryIndex: index('jquery-3.7.1.min.js'),
      sharedIndex: index('js/script.js'),
      ajaxIndex: index('js/ajax.js'),
      hasJQuery: typeof window.jQuery === 'function',
      hasAjax: typeof window.jQuery?.ajax === 'function'
    };
  });
}

test('existing login and logout remain role-aware with Identity user details', async ({ page }) => {
  await stubExternal(page);

  for (const role of ['Organizer', 'Treasurer']) {
    await login(page, role);
    await expect(page.locator('.profile-chip .name')).toHaveText(seededUsers[role].fullName);
    await page.click('.profile-chip');
    const menu = page.locator('.dropdown-menu.show');
    await expect(menu).toContainText(`${role} account`);
    await expect(menu).toContainText(seededUsers[role].email);
    await page.keyboard.press('Escape');
    await page.click('.profile-chip');
    await page.click('.dropdown-menu.show button[type=submit]');
    await expect(page).toHaveURL(/\/$/);
    await page.goto(`/${role}/Dashboard`);
    await expect(page).toHaveURL(/\/Identity\/Account\/Login/);
  }
});

test('contributor registration shows Identity FullName and email on dashboard and profile', async ({ page }) => {
  await stubExternal(page);
  const email = unique('identity-profile') + '@amanat.test';
  await registerContributor(page, email);
  await expect(page.locator('.profile-chip .name')).toHaveText('Profile Contributor');
  await page.click('.profile-chip');
  const menu = page.locator('.dropdown-menu.show');
  await expect(menu).toContainText('Contributor account');
  await expect(menu).toContainText(email);

  await page.goto('/Identity/Account/Manage/Profile');
  await expect(page.locator('#fullName')).toHaveValue('Profile Contributor');
  await expect(page.locator('#profile-email')).toHaveText(email);
  await expect(page.locator('#profile-role')).toHaveText('Contributor');
  await expect(page.locator('input[name="Input.Role"]')).toHaveCount(0);
});

test('profile updates are validated server-side and persist from the authenticated user', async ({ page }) => {
  await stubExternal(page);
  await registerContributor(page, unique('identity-update') + '@amanat.test');
  await page.goto('/Identity/Account/Manage/Profile');

  const invalid = await postProfileForm(page, { 'Input.FullName': '   ', 'Input.PhoneNumber': 'not-a-phone' });
  expect(invalid.status).toBe(200);
  expect(invalid.text).toContain('Please enter your full name.');
  expect(invalid.text).toContain('Please enter a valid phone number.');

  await page.fill('#fullName', 'Updated Profile Name');
  await page.fill('#phone', '03009876543');
  await page.click('button[type=submit]');
  await expect(page.locator('[data-profile-status]')).toContainText('Your profile has been updated.');

  await page.reload();
  await expect(page.locator('#fullName')).toHaveValue('Updated Profile Name');
  await expect(page.locator('#phone')).toHaveValue('03009876543');
  await expect(page.locator('.profile-chip .name')).toHaveCount(0);
});

test('profile editing cannot grant privileged roles', async ({ page }) => {
  await stubExternal(page);
  await registerContributor(page, unique('identity-role') + '@amanat.test');
  await page.goto('/Identity/Account/Manage/Profile');

  const forged = await postProfileForm(page, {
    'Input.FullName': 'Forged Organizer',
    'Input.PhoneNumber': '03001112222',
    'Input.Role': 'Organizer',
    'Role': 'Organizer'
  });
  expect(forged.status).toBe(200);

  await page.goto('/Identity/Account/Manage/Profile');
  await expect(page.locator('#profile-role')).toHaveText('Contributor');
  await expect(page.locator('#fullName')).toHaveValue('Forged Organizer');

  await page.goto('/Organizer/Dashboard');
  await expect(page).toHaveURL(/\/Home\/AccessDenied/);
});

test('optional layout sections render with and without page definitions', async ({ page }) => {
  await stubExternal(page);
  const responses = [];
  page.on('response', response => responses.push(response));

  await page.goto('/');
  expect(responses.some(response => response.url().endsWith('/') && response.status() === 200)).toBe(true);
  await expect(page.locator('footer.footer-amanat')).toHaveCount(1);
  await expect(page.locator('main.app-main > footer')).toHaveCount(0);
  const publicScripts = await scriptState(page);
  expect(publicScripts.jqueryCount).toBe(1);
  expect(publicScripts.jqueryLocal).toBe(true);
  expect(publicScripts.jqueryIndex).toBeLessThan(publicScripts.sharedIndex);
  expect(publicScripts.ajaxIndex).toBeGreaterThan(publicScripts.sharedIndex);
  expect(publicScripts.hasJQuery).toBe(true);
  expect(publicScripts.hasAjax).toBe(true);

  await login(page, 'Treasurer');
  await expect(page.locator('main.app-main > footer')).toHaveCount(1);
  const dashboardScripts = await scriptState(page);
  expect(dashboardScripts.jqueryCount).toBe(1);
  expect(dashboardScripts.ajaxIndex).toBeGreaterThan(dashboardScripts.sharedIndex);

  await page.goto('/Identity/Account/Manage/Profile');
  const profileScripts = await scriptState(page);
  expect(profileScripts.jqueryCount).toBe(1);
  expect(profileScripts.ajaxIndex).toBeGreaterThan(profileScripts.sharedIndex);

  await page.goto('/Treasurer/Ledger');
  await expect(page.locator('footer')).toHaveCount(1);
  const ledgerScripts = await scriptState(page);
  expect(ledgerScripts.jqueryCount).toBe(1);
});

test('AJAX still works after layout section changes', async ({ page }) => {
  await stubExternal(page);
  const summaryPromise = page.waitForResponse(response => response.url().includes('/Campaigns/Summary/'));
  await page.goto('/Campaigns/Details/education-for-all');
  const summaryResponse = await summaryPromise;
  expect(summaryResponse.status()).toBe(200);
  const summary = await summaryResponse.json();
  await expect(page.locator('#summary-raised')).toHaveText(summary.data.raisedText);
  const state = await scriptState(page);
  expect(state.jqueryCount).toBe(1);
  expect(state.hasAjax).toBe(true);
});
