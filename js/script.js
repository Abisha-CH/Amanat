/* ==========================================================================
   AMANAT (امانت) — Shared frontend interactions
   Vanilla JS + Bootstrap 5.3 (no build step)
   ========================================================================== */
(function () {
  'use strict';

  var PATHS = {
    organizer: 'organizer/dashboard.html',
    treasurer: 'treasurer/dashboard.html',
    contributor: 'contributor/dashboard.html'
  };

  function ready(fn) {
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', fn);
    } else {
      fn();
    }
  }

  function isNestedPage() {
    var path = window.location.pathname.replace(/\\/g, '/');
    return /\/(organizer|treasurer|contributor|public)\//i.test(path);
  }

  function withBase(path) {
    return isNestedPage() ? '../' + path : path;
  }

  function rolePath(role) {
    var key = String(role || '').toLowerCase();
    if (key.indexOf('organizer') !== -1) return withBase(PATHS.organizer);
    if (key.indexOf('treasurer') !== -1) return withBase(PATHS.treasurer);
    if (key.indexOf('contributor') !== -1) return withBase(PATHS.contributor);
    return withBase(PATHS.contributor);
  }

  /* ------------------------------------------------------------------
     Toast helper
     ------------------------------------------------------------------ */
  function showToast(message, variant) {
    if (!message) return;
    var toast = document.getElementById('amanat-toast');
    if (!toast) {
      toast = document.createElement('div');
      toast.id = 'amanat-toast';
      toast.className = 'amanat-toast';
      toast.setAttribute('role', 'status');
      toast.setAttribute('aria-live', 'polite');
      document.body.appendChild(toast);
    }
    toast.textContent = message;
    toast.style.background = variant === 'error' ? '#B91C1C' : '';
    toast.classList.add('show');
    window.clearTimeout(showToast._timer);
    showToast._timer = window.setTimeout(function () {
      toast.classList.remove('show');
    }, 3200);
  }
  window.showToast = showToast;

  /* ------------------------------------------------------------------
     Bootstrap validation interception
     ------------------------------------------------------------------ */
  function initValidation() {
    var forms = document.querySelectorAll('form.needs-validation');
    Array.prototype.forEach.call(forms, function (form) {
      form.addEventListener('submit', function (event) {
        event.preventDefault();
        event.stopPropagation();

        if (!form.checkValidity()) {
          form.classList.add('was-validated');
          return;
        }

        form.classList.add('was-validated');
        handleFormSuccess(form);
      });
    });
  }

  function handleFormSuccess(form) {
    var kind = form.dataset.form;

    if (kind === 'signup') {
      showToast('Account created. Redirecting to sign in…');
      window.setTimeout(function () {
        window.location.href = withBase('login.html');
      }, 900);
      return;
    }

    if (kind === 'login') {
      var selected = form.querySelector('.role-pill.active');
      var role = selected ? selected.dataset.role : 'Contributor';
      showToast('Signed in as ' + role + '. Redirecting…');
      window.setTimeout(function () {
        window.location.href = rolePath(role);
      }, 700);
      return;
    }

    if (kind === 'pledge') {
      showToast('Pledge recorded. Thank you for your support!');
      form.reset();
      form.classList.remove('was-validated');
      var target = document.getElementById('pledge-receipt');
      if (target) {
        target.classList.remove('d-none');
        target.textContent =
          'Your pledge has been logged to the public ledger and awaits mutual confirmation.';
      }
      return;
    }

    if (kind === 'expense') {
      showToast('Expense submitted for verification.');
      form.reset();
      form.classList.remove('was-validated');
      return;
    }

    showToast(form.dataset.successMessage || 'Submitted successfully.');
  }

  /* ------------------------------------------------------------------
     Password show / hide
     ------------------------------------------------------------------ */
  function initPasswordToggles() {
    Array.prototype.forEach.call(
      document.querySelectorAll('[data-toggle-password]'),
      function (button) {
        button.addEventListener('click', function () {
          var selector = button.getAttribute('data-toggle-password');
          var input = selector
            ? document.querySelector(selector)
            : button.closest('.input-icon-shell') && button.closest('.input-icon-shell').querySelector('input');
          if (!input) return;
          var showing = input.getAttribute('type') === 'text';
          input.setAttribute('type', showing ? 'password' : 'text');
          button.setAttribute('aria-label', showing ? 'Show password' : 'Hide password');
          var icon = button.querySelector('.bi');
          if (icon) {
            icon.classList.toggle('bi-eye', showing);
            icon.classList.toggle('bi-eye-slash', !showing);
          }
        });
      }
    );
  }

  /* ------------------------------------------------------------------
     Signup password matching
     ------------------------------------------------------------------ */
  function initPasswordMatch() {
    var password = document.getElementById('password');
    var confirm = document.getElementById('confirm-password');
    if (!password || !confirm) return;

    function check() {
      if (!confirm.value) {
        confirm.setCustomValidity('');
        return;
      }
      confirm.setCustomValidity(confirm.value === password.value ? '' : 'Passwords do not match.');
    }
    password.addEventListener('input', check);
    confirm.addEventListener('input', check);
  }

  /* ------------------------------------------------------------------
     Login role selector
     ------------------------------------------------------------------ */
  function initRoleSelector() {
    Array.prototype.forEach.call(document.querySelectorAll('.role-selector'), function (group) {
      var pills = group.querySelectorAll('.role-pill');
      Array.prototype.forEach.call(pills, function (pill) {
        pill.addEventListener('click', function () {
          Array.prototype.forEach.call(pills, function (p) {
            p.classList.remove('active');
            p.setAttribute('aria-pressed', 'false');
          });
          pill.classList.add('active');
          pill.setAttribute('aria-pressed', 'true');
        });
      });
    });
  }

  /* ------------------------------------------------------------------
     Campaign search / filter / sort
     ------------------------------------------------------------------ */
  function initCampaignFilters() {
    var grid = document.getElementById('campaign-grid');
    if (!grid) return;

    var cards = Array.prototype.slice.call(grid.querySelectorAll('[data-campaign]'));
    var search = document.getElementById('campaign-search');
    var sort = document.getElementById('sort-filter');
    var filters = document.querySelectorAll('[data-category-filter]');
    var empty = document.getElementById('campaign-empty');
    var count = document.getElementById('campaign-count');
    var activeCategory = 'all';

    function matches(card) {
      var term = search ? search.value.trim().toLowerCase() : '';
      if (!term) return true;
      var haystack = [
        card.dataset.title,
        card.dataset.org,
        card.dataset.category
      ].join(' ').toLowerCase();
      return haystack.indexOf(term) !== -1;
    }

    function applySort(list) {
      var mode = sort ? sort.value : 'featured';
      if (mode === 'featured' || !mode) return list;
      return list.sort(function (a, b) {
        var av, bv;
        if (mode === 'raised') {
          av = parseFloat(a.dataset.raised) || 0;
          bv = parseFloat(b.dataset.raised) || 0;
          return bv - av;
        }
        if (mode === 'newest') {
          av = a.dataset.date || '';
          bv = b.dataset.date || '';
          return bv.localeCompare(av);
        }
        if (mode === 'deadline') {
          av = a.dataset.deadline || '9999-12-31';
          bv = b.dataset.deadline || '9999-12-31';
          return av.localeCompare(bv);
        }
        return 0;
      });
    }

    function render() {
      var visible = 0;
      var matching = cards.filter(function (card) {
        var categoryOk =
          activeCategory === 'all' || card.dataset.category === activeCategory;
        return categoryOk && matches(card);
      });
      var ordered = applySort(matching.slice());

      cards.forEach(function (card) {
        card.parentElement.classList.add('d-none');
      });
      ordered.forEach(function (card) {
        card.parentElement.classList.remove('d-none');
        grid.appendChild(card.parentElement);
        visible += 1;
      });

      if (empty) empty.classList.toggle('d-none', visible !== 0);
      if (count) {
        count.textContent = visible + (visible === 1 ? ' campaign' : ' campaigns');
      }
    }

    if (search) {
      search.addEventListener('input', render);
    }
    if (sort) {
      sort.addEventListener('change', render);
    }
    Array.prototype.forEach.call(filters, function (pill) {
      pill.addEventListener('click', function () {
        Array.prototype.forEach.call(filters, function (p) {
          p.classList.remove('active');
          p.setAttribute('aria-pressed', 'false');
        });
        pill.classList.add('active');
        pill.setAttribute('aria-pressed', 'true');
        activeCategory = pill.dataset.categoryFilter || 'all';
        render();
      });
    });

    render();
  }

  /* ------------------------------------------------------------------
     Dashboard action buttons (demo state changes)
     ------------------------------------------------------------------ */
  function initActionButtons() {
    Array.prototype.forEach.call(document.querySelectorAll('.js-verify-btn'), function (btn) {
      btn.addEventListener('click', function () {
        var label = btn.dataset.label || 'Entry';
        showToast(label + ' verified and posted to the ledger.');
        var row = btn.closest('tr');
        if (row) {
          var badge = row.querySelector('.js-status');
          if (badge) {
            badge.className = 'badge-amanat badge-verified js-status';
            badge.textContent = 'Verified';
          }
        }
        btn.disabled = true;
        btn.classList.add('opacity-50');
      });
    });

    Array.prototype.forEach.call(document.querySelectorAll('.js-approve-btn'), function (btn) {
      btn.addEventListener('click', function () {
        showToast('Pledge approved and published.');
        var row = btn.closest('tr');
        if (row) {
          var badge = row.querySelector('.js-status');
          if (badge) {
            badge.className = 'badge-amanat badge-verified js-status';
            badge.textContent = 'Approved';
          }
        }
      });
    });

    Array.prototype.forEach.call(document.querySelectorAll('.js-reject-btn'), function (btn) {
      btn.addEventListener('click', function () {
        showToast('Pledge rejected.', 'error');
        var row = btn.closest('tr');
        if (row) {
          var badge = row.querySelector('.js-status');
          if (badge) {
            badge.className = 'badge-amanat badge-danger js-status';
            badge.textContent = 'Rejected';
          }
        }
      });
    });

    Array.prototype.forEach.call(document.querySelectorAll('.js-sign-btn'), function (btn) {
      btn.addEventListener('click', function () {
        btn.disabled = true;
        btn.classList.remove('btn-forest');
        btn.classList.add('btn-outline-forest');
        btn.innerHTML = '<i class="bi bi-check2-circle me-1"></i>Signed';
        showToast('Signature applied. Entries awaiting co-signature remain pending.');
      });
    });

    Array.prototype.forEach.call(document.querySelectorAll('.js-copy-btn'), function (btn) {
      btn.addEventListener('click', function () {
        var value = btn.dataset.copy || window.location.href;
        if (navigator.clipboard && navigator.clipboard.writeText) {
          navigator.clipboard.writeText(value).then(function () {
            showToast('Copied to clipboard.');
          }).catch(function () {
            showToast('Copy failed. Please copy manually.', 'error');
          });
        } else {
          showToast('Copied: ' + value);
        }
      });
    });
  }

  /* ------------------------------------------------------------------
     Ledger category filter
     ------------------------------------------------------------------ */
  function initLedgerFilter() {
    var select = document.getElementById('ledger-category-filter');
    if (!select) return;

    var rows = document.querySelectorAll('[data-ledger-row]');
    var visibleCount = document.getElementById('ledger-visible-count');

    function apply() {
      var value = select.value;
      var shown = 0;
      Array.prototype.forEach.call(rows, function (row) {
        var ok = value === 'all' || row.dataset.category === value;
        row.classList.toggle('is-hidden', !ok);
        if (ok) shown += 1;
      });
      if (visibleCount) visibleCount.textContent = String(shown);
    }

    select.addEventListener('change', apply);
    apply();
  }

  /* ------------------------------------------------------------------
     Demo placeholder links
     ------------------------------------------------------------------ */
  function initDemoLinks() {
    Array.prototype.forEach.call(document.querySelectorAll('.js-demo-link'), function (link) {
      link.addEventListener('click', function (event) {
        event.preventDefault();
        showToast(link.dataset.demoMessage || 'This feature is part of the interactive demo.');
      });
    });
  }

  /* ------------------------------------------------------------------
     Share / copy campaign link
     ------------------------------------------------------------------ */
  function initShareButtons() {
    Array.prototype.forEach.call(document.querySelectorAll('.js-share-btn'), function (btn) {
      btn.addEventListener('click', function () {
        var url = window.location.href;
        if (navigator.share) {
          navigator.share({
            title: document.title,
            url: url
          }).catch(function () {});
          return;
        }
        if (navigator.clipboard && navigator.clipboard.writeText) {
          navigator.clipboard.writeText(url).then(function () {
            showToast('Campaign link copied.');
          }).catch(function () {
            showToast('Unable to copy link.', 'error');
          });
        } else {
          showToast('Campaign link: ' + url);
        }
      });
    });
  }

  /* ------------------------------------------------------------------
     Smooth-scroll anchors for in-page nav
     ------------------------------------------------------------------ */
  function initAnchorScroll() {
    Array.prototype.forEach.call(
      document.querySelectorAll('a[href^="#"]:not([href="#"]):not(.js-demo-link)'),
      function (anchor) {
        anchor.addEventListener('click', function (event) {
          var id = anchor.getAttribute('href');
          var target = document.querySelector(id);
          if (!target) return;
          event.preventDefault();
          target.scrollIntoView({ behavior: 'smooth', block: 'start' });
        });
      }
    );
  }

  /* ------------------------------------------------------------------
     Boot
     ------------------------------------------------------------------ */
  ready(function () {
    initValidation();
    initPasswordToggles();
    initPasswordMatch();
    initRoleSelector();
    initCampaignFilters();
    initActionButtons();
    initLedgerFilter();
    initDemoLinks();
    initShareButtons();
    initAnchorScroll();
  });
})();
