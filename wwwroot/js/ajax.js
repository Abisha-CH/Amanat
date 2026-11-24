(function ($) {
  'use strict';

  if (!window.jQuery || !$.ajax) return;

  var TOKEN_NAME = '__RequestVerificationToken';

  function toast(message, isError) {
    if (window.showToast) window.showToast(message, isError ? 'error' : undefined);
  }

  function tokenFor(form) {
    var local = form.find('input[name="' + TOKEN_NAME + '"]').first().val();
    return local || $('input[name="' + TOKEN_NAME + '"]').first().val() || '';
  }

  function rememberFeedback(form) {
    form.find('.invalid-feedback').each(function () {
      var node = $(this);
      node.data('original-text', node.text());
    });
    form.find('[data-valmsg-for]').each(function () {
      var node = $(this);
      node.data('original-text', node.text());
      node.data('original-class', node.attr('class') || '');
    });
  }

  function clearErrors(form) {
    form.find('.is-invalid').removeClass('is-invalid');
    form.find('.invalid-feedback').each(function () {
      var node = $(this);
      var original = node.data('original-text');
      if (typeof original === 'string') node.text(original);
    });
    form.find('[data-valmsg-for]').each(function () {
      var node = $(this);
      node.text(typeof node.data('original-text') === 'string' ? node.data('original-text') : '');
      node.attr('class', typeof node.data('original-class') === 'string' ? node.data('original-class') : 'field-validation-valid');
    });
  }

  function applyErrors(form, errors) {
    clearErrors(form);
    $.each(errors || {}, function (field, messages) {
      var message = Array.isArray(messages) && messages.length ? messages[0] : String(messages || 'Please correct this value.');
      var input = form.find('[name="' + field + '"]').first();
      if (input.length) {
        input.addClass('is-invalid');
        input.siblings('.invalid-feedback').first().text(message);
      }
      var validation = form.find('[data-valmsg-for="' + field + '"]').first();
      if (validation.length) {
        validation.text(message).removeClass('field-validation-valid').addClass('field-validation-error');
      }
    });
  }

  function errorText(xhr, fallback) {
    var response = xhr.responseJSON;
    if (response && response.message) return response.message;
    if (xhr.status === 400) return 'Please check the submitted values.';
    if (xhr.status === 401) return 'Please sign in to continue.';
    if (xhr.status === 403) return 'You do not have permission to perform this action.';
    if (xhr.status === 404) return 'The requested record was not found.';
    if (xhr.status === 409) return 'This record has changed. Reload and try again.';
    if (xhr.status === 500) return 'The request could not be completed. Please try again.';
    return fallback || 'The request could not be completed.';
  }

  function busy(form, isBusy) {
    var button = form.find('button[type="submit"]').first();
    form.data('busy', isBusy);
    button.prop('disabled', isBusy).toggleClass('opacity-50', isBusy);
  }

  function updateReferenceHistory(data) {
    var section = $('#history-section');
    if (!section.length || !data) return;

    var badge = section.find('.badge-neutral').first();
    if (badge.length) {
      var match = badge.text().match(/\d+/);
      if (match) {
        badge.text(badge.text().replace(match[0], String(Number(match[0]) + 1)));
      }
    }

    var row = $('<tr>').attr('style', 'background: rgba(255,247,237,0.35);');
    $('<td>').addClass('text-muted-amanat text-nowrap').text(data.dateText).appendTo(row);
    $('<td>').addClass('cell-title text-nowrap').text(data.campaign).appendTo(row);
    $('<td>').addClass('fw-bold text-nowrap').text(data.amountText).appendTo(row);
    $('<td>').addClass('cell-mono text-nowrap').text(data.reference).appendTo(row);
    var statusCell = $('<td>').addClass('text-nowrap').appendTo(row);
    $('<span>').addClass('badge-amanat badge-pending')
      .append($('<span>').addClass('badge-dot'))
      .append(document.createTextNode(' ' + data.status))
      .appendTo(statusCell);
    var receiptCell = $('<td>').addClass('text-end text-nowrap').appendTo(row);
    $('<span>').addClass('text-muted-amanat fst-italic').css('font-size', '0.72rem').text('Pending').appendTo(receiptCell);
    section.find('tbody').first().prepend(row);
  }

  function decrementQueueBadge(form) {
    var badge = form.closest('section').find('.badge-pending').first();
    if (!badge.length) return;
    var match = badge.text().match(/\d+/);
    if (!match) return;
    badge.text(badge.text().replace(match[0], String(Math.max(0, Number(match[0]) - 1))));
  }

  function initCampaignSummary() {
    var panel = $('[data-campaign-summary]').first();
    if (!panel.length) return;

    $.get(panel.data('campaignSummaryUrl'), function (response) {
      if (!response || !response.success) return;
      var data = response.data;
      $('#summary-raised').text(data.raisedText);
      $('#summary-goal').text(data.goalText);
      $('#summary-total-received').text(data.raisedText);
      $('#summary-approved-expenses').text(data.approvedExpensesText);
      $('#summary-balance').text(data.balanceText);
      $('#summary-remaining').text(data.remainingText + ' Remaining');
      $('#summary-funded').text(data.progressPercent + '% Funded');
      $('#summary-contributors').text(data.contributorsText);
      $('#summary-approved-count').text(data.approvedCountText);
      $('#summary-progress-bar').css('width', data.progressPercent + '%');
      $('#campaign-status-text').text(data.status + ' Campaign');
      $('#campaign-status-badge')
        .removeClass('badge-verified badge-pending')
        .addClass(data.status === 'Active' ? 'badge-verified' : 'badge-pending');
    }, 'json');
  }

  function initContributorSubmit() {
    var form = $('form[data-ajax-submit="true"]');
    if (!form.length) return;
    rememberFeedback(form);

    form.on('submit', function (event) {
      if (event.isDefaultPrevented() || form.data('busy')) {
        if (form.data('busy')) event.preventDefault();
        return;
      }
      event.preventDefault();
      event.stopImmediatePropagation();

      clearErrors(form);
      busy(form, true);

      var payload = {};
      $.each(form.serializeArray(), function (_, item) {
        if (item.name !== TOKEN_NAME) payload[item.name] = item.value;
      });

      $.post({
        url: form.data('ajaxUrl'),
        contentType: 'application/json',
        dataType: 'json',
        data: JSON.stringify(payload),
        headers: { RequestVerificationToken: tokenFor(form) }
      }).done(function (response) {
        toast(response.message);
        if (response.success) {
          $('#reference-ajax-alert').text(response.message).removeClass('d-none');
          updateReferenceHistory(response.data);
          form[0].reset();
          form.removeClass('was-validated');
        }
      }).fail(function (xhr) {
        var response = xhr.responseJSON;
        form.find('#campaign-select').val(payload.Campaign);
        form.find('#amount-input').val(payload.Amount);
        form.find('#trx-id').val(payload.TransactionId);
        form.find('#date-sent').val(payload.DateSent);
        form.find('#notes').val(payload.Notes || '');
        if (response && response.errors) applyErrors(form, response.errors);
        toast(errorText(xhr, 'The reference could not be submitted.'), true);
      }).always(function () {
        busy(form, false);
      });
    });
  }

  function initDecisionButtons() {
    $('form[data-ajax-decision="true"]').on('submit', function (event) {
      if (event.isDefaultPrevented()) return;
      event.preventDefault();
      event.stopImmediatePropagation();

      var form = $(this);
      var row = form.closest('tr');
      if (row.data('busy')) return;
      row.data('busy', true);
      row.find('button').prop('disabled', true).addClass('opacity-50');

      var payload = {
        Decision: form.data('decision'),
        Note: ''
      };
      var contributionId = form.find('input[name="ContributionId"]').first().val();
      var expenseId = form.find('input[name="ExpenseId"]').first().val();
      if (contributionId) payload.ContributionId = Number(contributionId);
      if (expenseId) payload.ExpenseId = Number(expenseId);
      payload.RowVersion = form.find('input[name="RowVersion"]').first().val() || '';

      $.ajax({
        url: form.data('ajaxUrl'),
        type: 'POST',
        contentType: 'application/json',
        dataType: 'json',
        data: JSON.stringify(payload),
        headers: { RequestVerificationToken: tokenFor(form) }
      }).done(function (response) {
        if (!response || !response.success) return;
        var badge = row.find('.js-status').first();
        if (badge.length && response.data) {
          badge.removeClass('badge-pending badge-verified badge-danger badge-neutral badge-soft')
            .addClass('badge-amanat ' + response.data.statusClass + ' js-status')
            .empty()
            .append($('<span>').addClass('badge-dot'))
            .append(document.createTextNode(' ' + response.data.status));
        }
        decrementQueueBadge(form);
        toast(response.message);
      }).fail(function (xhr) {
        toast(errorText(xhr, 'The decision could not be saved.'), true);
      }).always(function () {
        row.data('busy', false);
        row.find('button').prop('disabled', false).removeClass('opacity-50');
      });
    });
  }

  $(function () {
    initCampaignSummary();
    initContributorSubmit();
    initDecisionButtons();
  });
})(window.jQuery);
