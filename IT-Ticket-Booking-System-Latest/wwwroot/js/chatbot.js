/**
 * IT Booking System — dual-mode chatbot (user quick-replies + engineer NLP assistant).
 */
(function () {
  const root = document.getElementById('chatbot-root');
  if (!root) return;

  const panel = document.getElementById('chatbot-panel');
  const launcher = document.getElementById('chatbot-launcher');
  const closeBtn = document.getElementById('chatbot-close');
  const messagesEl = document.getElementById('chatbot-messages');
  const userFooter = document.getElementById('chatbot-user-footer');
  const engineerFooter = document.getElementById('chatbot-engineer-footer');
  const quickReplies = document.getElementById('chatbot-quick-replies');
  const stepNav = document.getElementById('chatbot-step-nav');
  const escalate = document.getElementById('chatbot-escalate');
  const subtitle = document.getElementById('chatbot-subtitle');
  const titleEl = document.getElementById('chatbot-title');

  const state = {
    mode: root.dataset.role === 'engineer' ? 'engineer' : 'user',
    open: false,
    categories: [],
    createTicketUrl: '/Ticket/Create',
    steps: [],
    stepIndex: 0,
    currentGuide: null,
    configLoaded: false
  };

  function bubble(text, who) {
    const el = document.createElement('div');
    el.className = `chatbot-bubble ${who}`;
    el.textContent = text;
    messagesEl.appendChild(el);
    messagesEl.scrollTop = messagesEl.scrollHeight;
    return el;
  }

  function showTyping() {
    const el = document.createElement('div');
    el.className = 'chatbot-bubble bot typing';
    el.textContent = 'Assistant is thinking';
    messagesEl.appendChild(el);
    messagesEl.scrollTop = messagesEl.scrollHeight;
    return el;
  }

  async function loadConfig() {
    const res = await fetch('/Chatbot/config');
    if (!res.ok) return;
    const data = await res.json();
    state.mode = data.mode || state.mode;
    state.categories = data.categories || [];
    state.createTicketUrl = data.createTicketUrl || state.createTicketUrl;
    state.configLoaded = true;
  }

  function resetSession() {
    messagesEl.innerHTML = '';
    state.steps = [];
    state.stepIndex = 0;
    state.currentGuide = null;
    quickReplies.innerHTML = '';
    stepNav.classList.add('hidden');
    escalate.classList.add('hidden');
  }

  function renderUserHome() {
    resetSession();
    titleEl.textContent = 'IT Support Bot';
    subtitle.textContent = 'Choose a topic — no typing required';
    userFooter.classList.remove('hidden');
    engineerFooter.classList.add('hidden');

    bubble('Hi! I can guide you through common IT fixes before you open a ticket. Pick a topic below.', 'bot');

    quickReplies.innerHTML = '';
    state.categories.forEach((cat) => {
      const btn = document.createElement('button');
      btn.type = 'button';
      btn.className = 'chatbot-chip';
      btn.textContent = cat;
      btn.addEventListener('click', () => loadGuide(cat));
      quickReplies.appendChild(btn);
    });

    loadUserHistory();
  }

  async function loadUserHistory() {
    try {
      const res = await fetch('/Chatbot/user/history');
      if (!res.ok) return;
      const items = await res.json();
      if (!items.length) return;
      const wrap = document.createElement('div');
      wrap.className = 'chatbot-history';
      wrap.textContent = 'Recent: ' + items.map((h) => h.searchedIssue).slice(0, 4).join(' · ');
      messagesEl.appendChild(wrap);
    } catch (_) { /* ignore */ }
  }

  async function loadGuide(category) {
    bubble(category, 'user');
    const typing = showTyping();
    try {
      const res = await fetch(`/Chatbot/user/guide?category=${encodeURIComponent(category)}`);
      typing.remove();
      if (!res.ok) {
        bubble('Sorry, no guide is available for that topic yet.', 'bot');
        return;
      }
      const data = await res.json();
      state.currentGuide = data;
      state.steps = data.steps || [];
      state.stepIndex = 0;

      bubble(`${data.issueTitle} — follow each step. Use Next / Previous, then tell me if it is resolved.`, 'bot');
      quickReplies.innerHTML = '';
      stepNav.classList.remove('hidden');
      escalate.classList.add('hidden');
      renderStep();
    } catch {
      typing.remove();
      bubble('Network error. Please try again.', 'bot');
    }
  }

  function renderStep() {
    const existing = messagesEl.querySelector('.chatbot-step-card');
    if (existing) existing.remove();
    if (!state.steps.length) return;
    const card = document.createElement('div');
    card.className = 'chatbot-bubble bot chatbot-step-card';
    card.textContent = `Step ${state.stepIndex + 1} of ${state.steps.length}: ${state.steps[state.stepIndex]}`;
    messagesEl.appendChild(card);
    messagesEl.scrollTop = messagesEl.scrollHeight;

    document.getElementById('chatbot-prev').disabled = state.stepIndex === 0;
    document.getElementById('chatbot-next').disabled = state.stepIndex >= state.steps.length - 1;
  }

  function showResolutionPrompt() {
    bubble('Did these steps resolve your issue?', 'bot');
    stepNav.classList.remove('hidden');
    escalate.classList.add('hidden');
  }

  function renderEngineerHome() {
    resetSession();
    titleEl.textContent = 'Engineer AI Assistant';
    subtitle.textContent = 'TF-IDF + SVM category prediction';
    userFooter.classList.add('hidden');
    engineerFooter.classList.remove('hidden');
    bubble('Ask a technical question. I will predict the category and suggest fixes.', 'bot');
  }

  async function engineerAsk(query) {
    bubble(query, 'user');
    const typing = showTyping();
    const token = document.querySelector('#chatbot-engineer-form input[name="__RequestVerificationToken"]')?.value;
    const body = new URLSearchParams();
    body.append('query', query);
    if (token) body.append('__RequestVerificationToken', token);

    try {
      const res = await fetch('/Chatbot/engineer/ask', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: body.toString()
      });
      typing.remove();
      const data = await res.json();
      if (!res.ok) {
        bubble(data.message || 'Could not process your question.', 'bot');
        return;
      }
      bubble(data.summary, 'bot');
    } catch {
      typing.remove();
      bubble('AI service unreachable. Start the Flask ML service or enable fallback in appsettings.', 'bot');
    }
  }

  function openPanel() {
    panel.classList.remove('hidden');
    state.open = true;
    if (state.mode === 'engineer') renderEngineerHome();
    else renderUserHome();
  }

  function closePanel() {
    panel.classList.add('hidden');
    state.open = false;
    resetSession();
  }

  launcher.addEventListener('click', async () => {
    if (!state.configLoaded) await loadConfig();
    if (state.open) closePanel();
    else openPanel();
  });

  closeBtn.addEventListener('click', closePanel);

  document.getElementById('chatbot-prev').addEventListener('click', () => {
    if (state.stepIndex > 0) {
      state.stepIndex--;
      renderStep();
    }
  });

  document.getElementById('chatbot-next').addEventListener('click', () => {
    if (state.stepIndex < state.steps.length - 1) {
      state.stepIndex++;
      renderStep();
    } else {
      showResolutionPrompt();
    }
  });

  document.getElementById('chatbot-resolved-yes').addEventListener('click', () => {
    bubble('Great! Glad we could help. You can close this chat anytime.', 'bot');
    stepNav.classList.add('hidden');
    escalate.classList.add('hidden');
    setTimeout(closePanel, 1200);
  });

  document.getElementById('chatbot-resolved-no').addEventListener('click', () => {
    bubble('No problem — you can create a ticket with the category pre-filled.', 'bot');
    stepNav.classList.add('hidden');
    escalate.classList.remove('hidden');
  });

  document.getElementById('chatbot-create-ticket').addEventListener('click', () => {
    const g = state.currentGuide;
    if (!g) return;
    const params = new URLSearchParams({
      title: g.ticketPrefill?.title || g.category,
      description: g.ticketPrefill?.description || '',
      problemType: g.problemTypeValue ?? ''
    });
    window.location.href = `${state.createTicketUrl}?${params.toString()}`;
  });

  document.getElementById('chatbot-engineer-form').addEventListener('submit', (e) => {
    e.preventDefault();
    const input = document.getElementById('chatbot-engineer-input');
    const q = (input.value || '').trim();
    if (!q) return;
    input.value = '';
    engineerAsk(q);
  });

  loadConfig();
})();
