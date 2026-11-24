const { test, expect } = require('@playwright/test');

const baseURL = process.env.AMANAT_BASE_URL || 'http://127.0.0.1:4173';
const publicPassword = 'Amanat@2026';
const seededUsers = {
  Organizer: {
    email: process.env.AMANAT_SEED_ORGANIZER_EMAIL || 'organizer.seed@amanat.test',
    password: process.env.AMANAT_SEED_ORGANIZER_PASSWORD || 'SeedOrganizer@2026'
  },
  Treasurer: {
    email: process.env.AMANAT_SEED_TREASURER_EMAIL || 'treasurer.seed@amanat.test',
    password: process.env.AMANAT_SEED_TREASURER_PASSWORD || 'SeedTreasurer@2026'
  }
};
const publicPages = [
  '/',
  '/Home/About',
  '/Campaigns/Index',
  '/Campaigns/Details/education-for-all',
  '/Public/Ledger',
  '/Identity/Account/Login',
  '/Identity/Account/Register'
];

async function stubExternal(page) {
  await page.route('**/*', route => {
    const url = route.request().url();
    if (url.startsWith(baseURL)) return route.continue();
    if (url.includes('/bootstrap@5.3.3/dist/css/bootstrap.min.css')) {
      return route.fulfill({ status: 200, contentType: 'text/css', body: '' });
    }
    if (url.includes('/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js')) {
      return route.fulfill({ status: 200, contentType: 'application/javascript', body: '' });
    }
    if (url.includes('bootstrap-icons') || url.includes('fonts.googleapis.com') || url.includes('fonts.gstatic.com')) {
      return route.fulfill({ status: 200, contentType: 'text/css', body: '' });
    }
    return route.abort();
  });
}

async function watchErrors(page) {
  const errors = [];
  page.on('pageerror', error => errors.push(`pageerror: ${error.message}`));
  page.on('console', message => {
    if (message.type() === 'error' && !message.text().includes('favicon.ico')) {
      errors.push(`console: ${message.text()}`);
    }
  });
  page.on('response', response => {
    if (response.url().startsWith(baseURL) && response.status() >= 400 && !response.url().includes('favicon.ico')) {
      errors.push(`http ${response.status()}: ${response.url()}`);
    }
  });
  return errors;
}

async function registerContributor(page, email) {
  await page.goto('/Identity/Account/Register');
  await page.fill('#fullName', 'Contributor Tester');
  await page.fill('#email', email);
  await page.fill('#phone', '03001234567');
  await page.fill('#password', publicPassword);
  await page.fill('#confirm-password', publicPassword);
  await page.check('input[name="Input.Role"][value="Contributor"]');
  await page.check('#terms');
  await page.click('button[type=submit]');
  await expect(page).toHaveURL(/\/Contributor\/Dashboard$/);
}

async function login(page, email, password) {
  await page.goto('/Identity/Account/Login');
  await page.fill('#email', email);
  await page.fill('#password', password);
  await page.click('button[type=submit]');
  await expect(page).toHaveURL(/\/Dashboard$/);
}

async function logout(page) {
  await page.goto('/Contributor/Dashboard');
  await page.locator('form[action*="Logout"] button.sidebar-link').click();
  await expect(page).toHaveURL(baseURL + '/');
}

async function signInAs(page, role, email) {
  if (role === 'Contributor') {
    await registerContributor(page, email);
    return;
  }

  await login(page, seededUsers[role].email, seededUsers[role].password);
}

for (const path of publicPages) {
  test(`MVC page loads cleanly: ${path}`, async ({ page }) => {
    await stubExternal(page);
    const errors = await watchErrors(page);
    const response = await page.goto(path);
    expect(response.status()).toBe(200);
    await expect(page.locator('body')).toBeVisible();
    const brokenImages = await page.locator('img').evaluateAll((images, origin) =>
      images.filter(img => img.src.startsWith(origin) && (!img.complete || img.naturalWidth === 0)).map(img => img.src), baseURL
    );
    expect(brokenImages).toEqual([]);
    expect(errors).toEqual([]);
  });
}

test('registration rejects weak passwords and duplicate emails', async ({ page }) => {
  await stubExternal(page);
  const email = `spec-${Date.now()}@amanat.test`;
  await page.goto('/Identity/Account/Register');
  await page.fill('#fullName', 'Validation Tester');
  await page.fill('#email', email);
  await page.fill('#password', 'weak');
  await page.fill('#confirm-password', 'weak');
  await page.check('input[name="Input.Role"][value="Contributor"]');
  await page.check('#terms');
  await page.click('button[type=submit]');
  await expect(page.locator('body')).toContainText('Password must be at least 8 characters.');

  await registerContributor(page, email);
  await page.goto('/Identity/Account/Register');
  await page.fill('#fullName', 'Duplicate Tester');
  await page.fill('#email', email);
  await page.fill('#password', publicPassword);
  await page.fill('#confirm-password', publicPassword);
  await page.check('input[name="Input.Role"][value="Contributor"]');
  await page.check('#terms');
  await page.click('button[type=submit]');
  await expect(page.locator('body')).toContainText('already taken');
});

test('forged privileged registration is rejected server-side', async ({ browser }) => {
  for (const role of ['Organizer', 'Treasurer']) {
    const context = await browser.newContext({ baseURL });
    const page = await context.newPage();
    await stubExternal(page);
    const email = `forge-${role.toLowerCase()}-${Date.now()}-${Math.floor(Math.random() * 1000)}@amanat.test`;

    await page.goto('/Identity/Account/Register');
    await page.fill('#fullName', `Forged ${role}`);
    await page.fill('#email', email);
    await page.fill('#phone', '03001234567');
    await page.fill('#password', publicPassword);
    await page.fill('#confirm-password', publicPassword);
    await page.check('#terms');
    await page.evaluate(roleName => {
      const inputs = [...document.querySelectorAll('input[name="Input.Role"]')];
      inputs.forEach(input => {
        input.disabled = false;
        if (input.type === 'radio') {
          input.checked = input.value === roleName;
        } else {
          input.value = roleName;
        }
      });
      if (!inputs.length) {
        const input = document.createElement('input');
        input.type = 'hidden';
        input.name = 'Input.Role';
        input.value = roleName;
        document.querySelector('form[method="post"]').appendChild(input);
      }
    }, role);
    await page.click('button[type=submit]');

    await expect(page).toHaveURL(/\/Identity\/Account\/Register$/);
    await expect(page.locator('body')).toContainText('assigned by an administrator');

    await page.goto('/Identity/Account/Login');
    await page.fill('#email', email);
    await page.fill('#password', publicPassword);
    await page.click('button[type=submit]');
    await expect(page.locator('body')).toContainText('Invalid email or password.');
    await context.close();
  }
});

test('repeated failed logins lock the account', async ({ page }) => {
  await stubExternal(page);
  const email = `lockout-${Date.now()}@amanat.test`;
  await registerContributor(page, email);
  await logout(page);

  for (let attempt = 0; attempt < 5; attempt++) {
    await page.goto('/Identity/Account/Login');
    await page.fill('#email', email);
    await page.fill('#password', 'Wrong@2026');
    await page.click('button[type=submit]');
    await page.waitForLoadState('domcontentloaded');
  }

  await expect(page.locator('body')).toContainText('Your account is locked. Please try again later.');

  await page.goto('/Identity/Account/Login');
  await page.fill('#email', email);
  await page.fill('#password', publicPassword);
  await page.click('button[type=submit]');
  await expect(page.locator('body')).toContainText('Your account is locked. Please try again later.');
});

test('seeded organizer and treasurer accounts log in and stay in role', async ({ browser }) => {
  for (const role of ['Organizer', 'Treasurer']) {
    const context = await browser.newContext({ baseURL });
    const page = await context.newPage();
    await stubExternal(page);
    await login(page, seededUsers[role].email, seededUsers[role].password);
    await expect(page).toHaveURL(new RegExp(`/${role}/Dashboard$`));

    const response = await page.goto(`/${role}/Dashboard`);
    expect(response.status()).toBe(200);

    const otherRole = role === 'Organizer' ? 'Treasurer' : 'Organizer';
    await page.goto(`/${otherRole}/Dashboard`);
    expect(page.url()).toContain('/Home/AccessDenied');
    await context.close();
  }
});

test('login, logout and role authorization work', async ({ page, browser }) => {
  await stubExternal(page);
  const email = `auth-${Date.now()}@amanat.test`;
  await registerContributor(page, email);

  await page.goto('/Organizer/Dashboard');
  expect(page.url()).toContain('/Home/AccessDenied');
  await page.goto('/Treasurer/Dashboard');
  expect(page.url()).toContain('/Home/AccessDenied');
  await page.goto('/Treasurer/Ledger');
  expect(page.url()).toContain('/Home/AccessDenied');

  const anonymous = await browser.newContext({ baseURL });
  const anonymousPage = await anonymous.newPage();
  await stubExternal(anonymousPage);
  for (const path of ['/Contributor/Dashboard', '/Organizer/Dashboard', '/Treasurer/Dashboard', '/Treasurer/Ledger']) {
    const response = await anonymousPage.goto(path);
    expect(response.status()).toBe(200);
    expect(anonymousPage.url()).toContain('/Identity/Account/Login');
  }
  await anonymous.close();

  await logout(page);
  await page.goto('/Contributor/Dashboard');
  expect(page.url()).toContain('/Identity/Account/Login');
});

test('public pledge POST validates and preserves entered values', async ({ page }) => {
  await stubExternal(page);
  await page.goto('/Campaigns/Details/education-for-all');
  await page.fill('#pledge-name', '');
  await page.fill('#pledge-amount', '10');
  await page.fill('#pledge-ref', 'TRXSPEC001');
  await page.click('#pledge-form button[type=submit]');
  await expect(page.locator('#pledge-name')).toHaveValue('');
  await expect(page.locator('#pledge-amount')).toHaveValue('10');
  await expect(page.locator('#pledge-ref')).toHaveValue('TRXSPEC001');

  await page.fill('#pledge-name', 'Spec Tester');
  await page.fill('#pledge-amount', '2500');
  await page.fill('#pledge-ref', 'TRXSPEC002');
  await page.click('#pledge-form button[type=submit]');
  await expect(page).toHaveURL(/#pledge-section$/);
  await expect(page.locator('body')).toContainText('recorded for audit');
});

test('contributor and treasurer POST actions work and preserve invalid input', async ({ page }) => {
  await stubExternal(page);
  await signInAs(page, 'Contributor', `contrib-${Date.now()}@amanat.test`);
  await page.selectOption('#campaign-select', 'education-for-all');
  await page.fill('#amount-input', '7000');
  await page.fill('#trx-id', 'TRXSPEC003');
  await page.click('form[action*="RecordReference"] button[type=submit]');
  await expect(page.locator('body')).toContainText('submitted for treasurer verification');

  const treasurerPage = await page.context().newPage();
  await stubExternal(treasurerPage);
  await signInAs(treasurerPage, 'Treasurer');
  await treasurerPage.selectOption('#campaign-select', 'clean-water-initiative');
  await treasurerPage.fill('#expense-amount', '12000');
  await treasurerPage.fill('#payee-name', 'Spec Vendor');
  await treasurerPage.fill('#expense-desc', '');
  await treasurerPage.click('form[action*="SubmitExpense"] button[type=submit]');
  await expect(treasurerPage.locator('#payee-name')).toHaveValue('Spec Vendor');
  await expect(treasurerPage.locator('body')).toContainText('Please describe the expense.');

  await treasurerPage.fill('#expense-desc', 'Lab supplies');
  await treasurerPage.click('form[action*="SubmitExpense"] button[type=submit]');
  await expect(treasurerPage.locator('body')).toContainText('submitted for organizer approval');
});
