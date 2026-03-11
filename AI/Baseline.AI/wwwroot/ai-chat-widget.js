/**
 * AI Chat Widget - Client-side functionality
 */
(function () {
  "use strict";

  class AIChatWidget {
    constructor(element) {
      this.element = element;
      this.kbId = element.dataset.kbId;
      this.theme = element.dataset.theme || "light";
      this.position = element.dataset.position || "bottom-right";
      this.initialExpanded = element.dataset.initialExpanded === "true";

      this.sessionId = this.getOrCreateSessionId();
      this.isExpanded = this.initialExpanded;

      this.toggleBtn = element.querySelector(".ai-chat-toggle");
      this.chatWindow = element.querySelector(".ai-chat-window");
      this.closeBtn = element.querySelector(".ai-chat-close");
      this.messagesContainer = element.querySelector(".ai-chat-messages");
      this.inputForm = element.querySelector(".ai-chat-input-form");
      this.input = element.querySelector(".ai-chat-input");
      this.sendBtn = element.querySelector(".ai-chat-send");
      this.loadingIndicator = element.querySelector(".ai-chat-loading");

      this.initializeWidget();
    }

    initializeWidget() {
      // Apply position
      this.element.classList.add(`ai-chat-position-${this.position}`);
      this.element.classList.add(`ai-chat-theme-${this.theme}`);

      // Show window if initially expanded
      if (this.initialExpanded) {
        this.chatWindow.style.display = "flex";
        this.toggleBtn.style.display = "none";
      }

      // Bind events
      this.toggleBtn.addEventListener("click", () => this.toggleChat());
      this.closeBtn.addEventListener("click", () => this.toggleChat());
      this.inputForm.addEventListener("submit", (e) => this.handleSubmit(e));
      this.input.addEventListener("input", () => this.handleInputChange());

      // Bind suggestion buttons
      const suggestions = this.element.querySelectorAll(".ai-chat-suggestion");
      suggestions.forEach((btn) => {
        btn.addEventListener("click", () => {
          const question = btn.dataset.question;
          if (question) {
            this.input.value = question;
            this.handleSubmit(new Event("submit"));
          }
        });
      });

      // Auto-focus input when chat opens
      this.chatWindow.addEventListener("transitionend", () => {
        if (this.isExpanded) {
          this.input.focus();
        }
      });
    }

    toggleChat() {
      this.isExpanded = !this.isExpanded;

      if (this.isExpanded) {
        this.chatWindow.style.display = "flex";
        this.toggleBtn.style.display = "none";
        setTimeout(() => {
          this.chatWindow.classList.add("ai-chat-window-open");
          this.input.focus();
        }, 10);
      } else {
        this.chatWindow.classList.remove("ai-chat-window-open");
        setTimeout(() => {
          this.chatWindow.style.display = "none";
          this.toggleBtn.style.display = "flex";
        }, 300);
      }
    }

    handleInputChange() {
      this.sendBtn.disabled = this.input.value.trim().length === 0;
    }

    async handleSubmit(e) {
      e.preventDefault();

      const message = this.input.value.trim();
      if (!message) return;

      // Add user message
      this.addMessage(message, "user");
      this.input.value = "";
      this.sendBtn.disabled = true;

      // Show loading
      this.setLoading(true);

      try {
        await this.sendMessageStream(message);
      } catch (error) {
        console.error("Chat error:", error);
        this.addMessage(
          "Sorry, I encountered an error. Please try again.",
          "bot",
          true,
        );
      } finally {
        this.setLoading(false);
      }
    }

    async sendMessageStream(message) {
      const response = await fetch("/api/ai/chat/stream", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          message: message,
          sessionId: this.sessionId,
          knowledgeBaseId: parseInt(this.kbId),
        }),
      });

      if (!response.ok) {
        throw new Error("Network response was not ok");
      }

      const reader = response.body.getReader();
      const decoder = new TextDecoder();
      let messageElement = null;
      let currentContent = "";
      let buffer = "";
      let currentEvent = "";
      let currentData = "";

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split("\n");
        // Keep incomplete last line in buffer
        buffer = lines.pop() || "";

        for (const line of lines) {
          if (line.startsWith("event: ")) {
            currentEvent = line.substring(7).trim();
          } else if (line.startsWith("data: ")) {
            currentData = line.substring(6);
          } else if (line.trim() === "" && currentEvent) {
            // Blank line = end of SSE message, dispatch
            if (currentEvent === "content") {
              if (!messageElement) {
                messageElement = this.createMessageElement("", "bot");
                this.messagesContainer.appendChild(messageElement);
              }
              currentContent += currentData;
              const contentElement = messageElement.querySelector(
                ".ai-chat-message-content",
              );
              contentElement.innerHTML = this.renderMarkdown(currentContent);
              this.scrollToBottom();
            } else if (currentEvent === "sources" && currentData) {
              try {
                const sources = JSON.parse(currentData);
                if (sources && sources.length > 0 && messageElement) {
                  this.addSources(messageElement, sources);
                }
              } catch (e) {
                console.error("Error parsing sources:", e);
              }
            } else if (currentEvent === "done") {
              // Add feedback buttons to the bot message
              if (messageElement) {
                this.addFeedbackButtons(messageElement);
              }
              // Hide starter suggestions after first answer
              const suggestions = this.element.querySelector(
                ".ai-chat-suggestions",
              );
              if (suggestions) suggestions.style.display = "none";
              currentEvent = "";
              currentData = "";
              return;
            } else if (currentEvent === "suggestions" && currentData) {
              try {
                const questions = JSON.parse(currentData);
                if (questions && questions.length > 0) {
                  this.renderFollowUpSuggestions(questions);
                }
              } catch (e) {
                console.error("Error parsing suggestions:", e);
              }
            } else if (currentEvent === "error") {
              throw new Error(currentData);
            }
            currentEvent = "";
            currentData = "";
          }
        }
      }
    }

    addMessage(content, type, isError = false) {
      const messageElement = this.createMessageElement(content, type, isError);
      this.messagesContainer.appendChild(messageElement);
      this.scrollToBottom();
      return messageElement;
    }

    createMessageElement(content, type, isError = false) {
      const div = document.createElement("div");
      div.className = `ai-chat-message ai-chat-message-${type}`;
      if (isError) div.classList.add("ai-chat-message-error");

      const contentDiv = document.createElement("div");
      contentDiv.className = "ai-chat-message-content";
      contentDiv.textContent = content;

      div.appendChild(contentDiv);
      return div;
    }

    addSources(messageElement, sources) {
      const sourcesDiv = document.createElement("div");
      sourcesDiv.className = "ai-chat-sources";
      sourcesDiv.innerHTML = "<strong>Sources:</strong>";

      const ul = document.createElement("ul");
      sources.forEach((source) => {
        const li = document.createElement("li");
        if (source.url) {
          const a = document.createElement("a");
          a.href = source.url;
          a.textContent = source.title || source.url;
          a.target = "_blank";
          a.rel = "noopener noreferrer";
          li.appendChild(a);
        } else {
          li.textContent = source.title || "Untitled";
        }

        if (source.snippet) {
          const snippet = document.createElement("span");
          snippet.className = "ai-chat-source-snippet";
          snippet.textContent = ` - ${source.snippet}`;
          li.appendChild(snippet);
        }

        ul.appendChild(li);
      });

      sourcesDiv.appendChild(ul);
      messageElement.appendChild(sourcesDiv);
    }

    addFeedbackButtons(messageElement) {
      const feedbackDiv = document.createElement("div");
      feedbackDiv.className = "ai-chat-feedback";

      const label = document.createElement("span");
      label.className = "ai-chat-feedback-label";
      label.textContent = "Helpful?";
      feedbackDiv.appendChild(label);

      const thumbsUp = document.createElement("button");
      thumbsUp.className = "ai-chat-feedback-btn ai-chat-feedback-positive";
      thumbsUp.innerHTML = "&#128077;";
      thumbsUp.title = "Helpful";
      thumbsUp.addEventListener("click", () =>
        this.submitFeedback(messageElement, true, thumbsUp, thumbsDown),
      );

      const thumbsDown = document.createElement("button");
      thumbsDown.className = "ai-chat-feedback-btn ai-chat-feedback-negative";
      thumbsDown.innerHTML = "&#128078;";
      thumbsDown.title = "Not helpful";
      thumbsDown.addEventListener("click", () =>
        this.submitFeedback(messageElement, false, thumbsUp, thumbsDown),
      );

      feedbackDiv.appendChild(thumbsUp);
      feedbackDiv.appendChild(thumbsDown);
      messageElement.appendChild(feedbackDiv);
    }

    async submitFeedback(messageElement, isHelpful, thumbsUp, thumbsDown) {
      // Visual toggle
      thumbsUp.classList.toggle("ai-chat-feedback-selected", isHelpful);
      thumbsDown.classList.toggle("ai-chat-feedback-selected", !isHelpful);

      // Disable further clicks
      thumbsUp.disabled = true;
      thumbsDown.disabled = true;

      try {
        await fetch("/api/ai/chat/feedback", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            sessionId: this.sessionId,
            messageId: messageElement.dataset.messageId || "",
            isHelpful: isHelpful,
          }),
        });
      } catch (error) {
        console.error("Feedback submission failed:", error);
      }
    }

    renderFollowUpSuggestions(questions) {
      // Remove any existing follow-up suggestions
      const existing =
        this.messagesContainer.querySelector(".ai-chat-followups");
      if (existing) existing.remove();

      const container = document.createElement("div");
      container.className = "ai-chat-followups";

      questions.forEach((q) => {
        const btn = document.createElement("button");
        btn.className = "ai-chat-suggestion";
        btn.textContent = q;
        btn.addEventListener("click", () => {
          container.remove();
          this.input.value = q;
          this.handleSubmit(new Event("submit"));
        });
        container.appendChild(btn);
      });

      this.messagesContainer.appendChild(container);
      this.scrollToBottom();
    }

    setLoading(isLoading) {
      if (isLoading) {
        this.loadingIndicator.style.display = "flex";
        this.input.disabled = true;
      } else {
        this.loadingIndicator.style.display = "none";
        this.input.disabled = false;
        this.input.focus();
      }
    }

    scrollToBottom() {
      this.messagesContainer.scrollTop = this.messagesContainer.scrollHeight;
    }

    /**
     * Lightweight markdown rendering for bot responses.
     * Handles: bold, italic, links, code blocks, inline code, line breaks.
     */
    renderMarkdown(text) {
      if (!text) return "";
      let html = text
        // Escape HTML
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        // Code blocks (```...```)
        .replace(
          /```(\w*)\n?([\s\S]*?)```/g,
          '<pre><code class="ai-code-block">$2</code></pre>',
        )
        // Inline code (`...`)
        .replace(/`([^`]+)`/g, "<code>$1</code>")
        // Bold (**...**)
        .replace(/\*\*(.+?)\*\*/g, "<strong>$1</strong>")
        // Italic (*...*)
        .replace(/\*(.+?)\*/g, "<em>$1</em>")
        // Links [text](url)
        .replace(
          /\[([^\]]+)\]\((https?:\/\/[^)]+)\)/g,
          '<a href="$2" target="_blank" rel="noopener noreferrer">$1</a>',
        )
        // Line breaks
        .replace(/\n/g, "<br>");
      return html;
    }

    getOrCreateSessionId() {
      const key = "ai-chat-session-id";
      let sessionId = sessionStorage.getItem(key);

      if (!sessionId) {
        sessionId = this.generateSessionId();
        sessionStorage.setItem(key, sessionId);
      }

      return sessionId;
    }

    generateSessionId() {
      return `chat-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    }
  }

  // Initialize all chat widgets on page load
  document.addEventListener("DOMContentLoaded", function () {
    const widgets = document.querySelectorAll(".ai-chat-widget");
    widgets.forEach((widget) => new AIChatWidget(widget));
  });
})();
