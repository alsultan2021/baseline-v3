using System.ComponentModel;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Options;

using ModelContextProtocol.Server;

namespace Baseline.Core.MCP.Tools;

/// <summary>
/// Crafto HTML design helper tools — returns component snippets, utility class
/// references, layout patterns, demo theme catalogs and section composition
/// guidance so agents can produce Crafto-compliant markup without guessing.
/// </summary>
[McpServerToolType]
public static class CraftoDesignTool
{
    // ───────────────────────── static catalog data ─────────────────────────

    #region Component Catalog

    private static readonly Dictionary<string, ComponentInfo> Components = new(StringComparer.OrdinalIgnoreCase)
    {
        ["icon-with-text"] = new(
            Name: "Icon With Text",
            File: "element-icon-with-text.html",
            Styles: ["icon-with-text-style-01", "icon-with-text-style-02", "icon-with-text-style-03", "icon-with-text-style-04", "icon-with-text-style-05", "icon-with-text-style-06", "icon-with-text-style-07", "icon-with-text-style-08", "icon-with-text-style-09", "icon-with-text-style-10"],
            KeyClasses: ["feature-box", "feature-box-left-icon", "feature-box-icon", "feature-box-content", "last-paragraph-no-margin", "icon-large", "icon-extra-large"],
            Category: "Content",
            Description: "Icon + text feature boxes in various layouts (left-icon, top-icon, card, dark-hover).",
            Snippets: new Dictionary<string, string>
            {
                ["style-01"] = """
<div class="col icon-with-text-style-01 mb-50px sm-mb-40px">
  <div class="feature-box feature-box-left-icon last-paragraph-no-margin">
    <div class="feature-box-icon">
      <i class="line-icon-Navigation-LeftWindow icon-large text-dark-gray"></i>
    </div>
    <div class="feature-box-content">
      <span class="d-inline-block text-dark-gray fw-600 mb-5px">Amazing layouts</span>
      <p class="w-80 xl-w-90">Lorem ipsum is simply dummy text of the printing typesetting.</p>
    </div>
  </div>
</div>
""",
                ["style-07"] = """
<div class="col icon-with-text-style-07 transition-inner-all mb-30px">
  <div class="hover-box dark-hover feature-box p-55px lg-p-30px overflow-hidden bg-dark-slate-blue text-start box-shadow-double-large-hover">
    <div class="feature-box-icon min-h-150px sm-min-h-100px mb-20 z-index-9">
      <i class="line-icon-Management icon-extra-large text-white"></i>
    </div>
    <div class="feature-box-title fs-200 fw-600 text-black opacity-2 ls-minus-10px">design</div>
    <div class="feature-box-content last-paragraph-no-margin">
      <span class="d-inline-block text-white mb-5px fs-18">Design research</span>
      <p class="w-90 xl-w-100 lh-30 text-light-opacity">We create compelling web designs for your target groups.</p>
    </div>
    <div class="feature-box-overlay bg-base-color"></div>
  </div>
</div>
"""
            }),

        ["buttons"] = new(
            Name: "Buttons",
            File: "element-buttons.html",
            Styles: [],
            KeyClasses: ["btn", "btn-extra-large", "btn-large", "btn-medium", "btn-small", "btn-very-small", "btn-round-edge", "btn-rounded", "btn-base-color", "btn-dark-gray", "btn-black", "btn-white", "btn-transparent-base-color", "btn-transparent-dark-gray", "btn-transparent-light-gray", "btn-gradient-sky-blue-pink", "btn-gradient-purple-pink", "btn-switch-text", "btn-slide-down", "btn-hover-animation", "btn-box-shadow", "btn-link"],
            Category: "Interactive",
            Description: "All button sizes, shapes (default/round-edge/rounded), solid/border/gradient variants, switch-text animation.",
            Snippets: new Dictionary<string, string>
            {
                ["solid-sizes"] = """
<a href="#" class="btn btn-base-color btn-extra-large">Button Extra Large</a>
<a href="#" class="btn btn-base-color btn-large">Button Large</a>
<a href="#" class="btn btn-base-color btn-medium">Button Medium</a>
<a href="#" class="btn btn-base-color btn-small">Button Small</a>
<a href="#" class="btn btn-base-color btn-very-small">Very Small</a>
""",
                ["shapes"] = """
<a href="#" class="btn btn-dark-gray btn-large">Button Default</a>
<a href="#" class="btn btn-dark-gray btn-large btn-round-edge">Button Round Edges</a>
<a href="#" class="btn btn-dark-gray btn-large btn-rounded">Button Rounded</a>
""",
                ["border-only"] = """
<a href="#" class="btn btn-extra-large btn-transparent-base-color">Border base color</a>
<a href="#" class="btn btn-extra-large btn-round-edge btn-transparent-light-gray">Border light</a>
<a href="#" class="btn btn-extra-large btn-rounded btn-transparent-dark-gray">Border dark gray</a>
""",
                ["switch-text"] = """
<a href="#" class="btn btn-medium btn-switch-text btn-rounded btn-base-color btn-box-shadow">
  <span><span class="btn-double-text" data-text="Explore more">Explore more</span></span>
</a>
"""
            }),

        ["fancy-text-box"] = new(
            Name: "Fancy Text Box",
            File: "element-fancy-text-box.html",
            Styles: ["fancy-text-box-style-01", "fancy-text-box-style-02"],
            KeyClasses: ["text-box-wrapper", "text-box", "text-box-hover", "border-color-light-medium-gray"],
            Category: "Content",
            Description: "Hover-reveal text boxes with icon + title + hidden content.",
            Snippets: new Dictionary<string, string>
            {
                ["style-01"] = """
<div class="col fancy-text-box-style-01 border-color-light-medium-gray p-0">
  <div class="text-box-wrapper align-items-center d-flex">
    <div class="position-relative text-center w-100">
      <div class="text-box last-paragraph-no-margin p-14">
        <i class="line-icon-Cursor-Click2 icon-extra-large d-block mb-20px text-base-color"></i>
        <span class="text-dark-gray fw-600 fs-22 lg-fs-20">Strategies</span>
        <p>Lorem ipsum dolor</p>
      </div>
      <div class="text-box-hover p-16 lg-p-12 md-p-17 sm-p-12 xs-p-17">
        <p class="mb-15px">Lorem ipsum dolor consectetur adipiscing do eiusmod tempor.</p>
        <a href="#" class="btn btn-link btn-large text-base-color thin fw-700">Consulting services</a>
      </div>
    </div>
  </div>
</div>
"""
            }),

        ["pricing-table"] = new(
            Name: "Pricing Table",
            File: "element-pricing-table.html",
            Styles: ["pricing-table-style-01", "pricing-table-style-02", "pricing-table-style-03", "pricing-table-style-04"],
            KeyClasses: ["pricing-table", "pricing-header", "pricing-body", "pricing-footer", "popular-item", "list-style-01"],
            Category: "Commerce",
            Description: "Pricing cards with header/body/footer structure; add popular-item for highlighted tier.",
            Snippets: new Dictionary<string, string>
            {
                ["style-01"] = """
<div class="col-lg-4 col-md-8 pricing-table-style-01">
  <div class="pricing-table text-center p-15">
    <div class="pricing-header">
      <div class="d-inline-block alt-font fw-600 text-base-color text-uppercase">Basic</div>
      <h3 class="text-dark-gray mb-0 fw-600 ls-minus-2px"><sup class="fs-26">$</sup>19.99</h3>
      <div class="fs-14 lh-20">monthly billing</div>
    </div>
    <div class="pricing-body pb-30px pt-20px">
      <ul class="list-style-01 ps-0 mb-0">
        <li class="border-color-transparent-dark-very-light pt-10px pb-10px">Marketing strategy</li>
        <li class="border-color-transparent-dark-very-light pt-10px pb-10px">Email & live chat support</li>
        <li class="pt-10px pb-10px">Social media share audit</li>
      </ul>
    </div>
    <div class="pricing-footer">
      <a href="#" class="btn btn-dark-gray btn-medium btn-rounded">Get started</a>
    </div>
  </div>
</div>
"""
            }),

        ["team"] = new(
            Name: "Team",
            File: "element-team.html",
            Styles: ["team-style-01", "team-style-02", "team-style-03", "team-style-04", "team-style-05"],
            KeyClasses: ["hover-box", "box-hover", "box-overlay", "social-icon", "social-icon-style-05"],
            Category: "Content",
            Description: "Team member cards with image, name, role, and hover-reveal social links.",
            Snippets: new Dictionary<string, string>
            {
                ["style-01"] = """
<div class="col text-center team-style-01 md-mb-30px">
  <figure class="mb-0 hover-box box-hover position-relative">
    <img src="https://placehold.co/600x736" alt="" class="border-radius-6px" />
    <figcaption class="w-100 p-30px lg-p-20px bg-white">
      <div class="position-relative z-index-1 overflow-hidden lg-pb-5px">
        <span class="fs-18 d-block fw-600 text-dark-gray lh-26 ls-minus-05px">Jeremy dupont</span>
        <p class="m-0">Executive officer</p>
        <div class="social-icon hover-text mt-20px lg-mt-10px social-icon-style-05">
          <a href="#" target="_blank" class="fw-600 text-dark-gray">Fb.</a>
          <a href="#" target="_blank" class="fw-600 text-dark-gray">In.</a>
          <a href="#" target="_blank" class="fw-600 text-dark-gray">Tw.</a>
        </div>
      </div>
      <div class="box-overlay bg-white box-shadow-quadruple-large border-radius-6px"></div>
    </figcaption>
  </figure>
</div>
"""
            }),

        ["carousel"] = new(
            Name: "Carousel (Swiper)",
            File: "element-carousel.html",
            Styles: [],
            KeyClasses: ["swiper", "swiper-wrapper", "swiper-slide", "swiper-pagination", "swiper-dark-pagination", "magic-cursor", "drag-cursor"],
            Category: "Layout",
            Description: "Swiper-based carousels with JSON data-slider-options for slidesPerView, breakpoints, effects.",
            Snippets: new Dictionary<string, string>
            {
                ["basic-image"] = """
<div class="swiper-dark-pagination magic-cursor drag-cursor">
  <div class="swiper overflow-visible" data-slider-options='{ "slidesPerView": 1, "spaceBetween": 40, "centeredSlides": true, "loop": true, "pagination": { "el": ".swiper-pagination-bullets-01", "clickable": true }, "breakpoints": { "992": { "slidesPerView": 1.8 }, "768": { "slidesPerView": 1.8 }, "320": { "slidesPerView": 1.3 } }, "effect": "slide" }'>
    <div class="swiper-wrapper align-items-center">
      <div class="swiper-slide"><img class="border-radius-6px w-100" src="https://placehold.co/1050x645" alt="" /></div>
      <div class="swiper-slide"><img class="border-radius-6px w-100" src="https://placehold.co/1050x645" alt="" /></div>
      <div class="swiper-slide"><img class="border-radius-6px w-100" src="https://placehold.co/1050x645" alt="" /></div>
    </div>
  </div>
  <div class="swiper-pagination swiper-pagination-clickable swiper-pagination-style-01 position-static mt-40px"></div>
</div>
"""
            }),

        ["testimonials"] = new(
            Name: "Testimonials",
            File: "element-testimonials.html",
            Styles: ["testimonials-style-01", "testimonials-style-02", "testimonials-style-03", "testimonials-style-04", "testimonials-style-05", "testimonials-style-06"],
            KeyClasses: ["testimonial-arrow", "box-shadow-quadruple-large", "border-radius-10px"],
            Category: "Social Proof",
            Description: "Testimonial cards with speech-bubble arrow, avatar, name/role.",
            Snippets: new Dictionary<string, string>
            {
                ["style-01"] = """
<div class="col testimonials-style-01 md-mb-30px">
  <div class="position-relative bg-white testimonial-arrow ps-50px pe-50px pt-30px pb-30px lg-p-30px border-radius-10px box-shadow-quadruple-large">
    <span>Lorem ipsum dolor amet ipsum adipiscing elit eiusmod tempor incididunt.</span>
  </div>
  <div class="mt-10px pt-20px pb-20px ps-15px pe-15px">
    <img src="https://placehold.co/200x200" class="w-80px h-80px rounded-circle me-15px" alt="" />
    <div class="d-inline-block align-middle lh-20">
      <div class="fw-600 text-dark-gray fs-17 mb-5px">Herman miller</div>
      <span class="fs-15">Chief financial</span>
    </div>
  </div>
</div>
"""
            }),

        ["counters"] = new(
            Name: "Counters",
            File: "element-counters.html",
            Styles: ["counter-style-01", "counter-style-02", "counter-style-03", "counter-style-04", "counter-style-05"],
            KeyClasses: ["counter-number", "counter", "vertical-counter", "feature-box"],
            Category: "Data Display",
            Description: "Animated counter numbers with icon. Use data-speed and data-to attributes.",
            Snippets: new Dictionary<string, string>
            {
                ["style-01"] = """
<div class="row row-cols-1 row-cols-sm-2 row-cols-lg-4 justify-content-center counter-style-01">
  <div class="col feature-box md-mb-50px xs-mb-30px">
    <div class="feature-box-icon">
      <i class="ti-crown icon-large text-dark-gray mb-20px d-block"></i>
    </div>
    <div class="feature-box-content">
      <h2 class="d-inline-block align-middle counter-number fw-700 text-dark-gray mb-0 counter" data-speed="2000" data-to="2350"></h2>
      <span class="d-block text-dark-gray fw-500">Global brands</span>
    </div>
  </div>
</div>
"""
            }),

        ["tabs"] = new(
            Name: "Tabs",
            File: "element-tab.html",
            Styles: ["tab-style-01", "tab-style-02", "tab-style-03", "tab-style-04", "tab-style-05", "tab-style-06", "tab-style-07", "tab-style-08"],
            KeyClasses: ["nav-tabs", "tab-content", "tab-pane", "nav-link"],
            Category: "Navigation",
            Description: "Bootstrap 5 tabs with Crafto styling; horizontal and vertical variants.",
            Snippets: new Dictionary<string, string>
            {
                ["style-01"] = """
<div class="col tab-style-01">
  <ul class="nav nav-tabs justify-content-center border-0 text-center fs-18 alt-font fw-600 mb-3">
    <li class="nav-item"><a class="nav-link active" data-bs-toggle="tab" href="#tab_sec1">Planning</a></li>
    <li class="nav-item"><a class="nav-link" data-bs-toggle="tab" href="#tab_sec2">Research</a></li>
    <li class="nav-item"><a class="nav-link" data-bs-toggle="tab" href="#tab_sec3">Target</a></li>
  </ul>
  <div class="tab-content">
    <div class="tab-pane fade in active show" id="tab_sec1">
      <div class="row justify-content-center align-items-center">
        <div class="col-md-6 sm-mb-50px"><img src="https://placehold.co/580x535" alt="" /></div>
        <div class="col-lg-5 offset-lg-1 col-md-6 text-center text-md-start">
          <span class="ps-20px pe-20px mb-25px text-uppercase text-cornflower-blue fs-13 lh-40 border-radius-100px alt-font fw-700 bg-solitude-blue d-inline-block">Working process</span>
          <h3 class="alt-font text-dark-gray fw-700 ls-minus-1px">Simple working process.</h3>
          <p class="w-80 mb-30px">Description text here.</p>
          <a href="#" class="btn btn-medium btn-switch-text btn-rounded btn-base-color btn-box-shadow">
            <span><span class="btn-double-text" data-text="Explore more">Explore more</span></span>
          </a>
        </div>
      </div>
    </div>
  </div>
</div>
"""
            }),

        ["accordion"] = new(
            Name: "Accordion",
            File: "element-accordion.html",
            Styles: ["accordion-style-01", "accordion-style-02", "accordion-style-03", "accordion-style-04", "accordion-style-05", "accordion-style-06"],
            KeyClasses: ["accordion", "accordion-item", "accordion-header", "accordion-collapse", "accordion-body", "active-accordion"],
            Category: "Content",
            Description: "FAQ-style collapsible panels; uses data-active-icon/data-inactive-icon attributes.",
            Snippets: new Dictionary<string, string>
            {
                ["style-01"] = """
<div class="accordion accordion-style-01" id="accordion-style-01" data-active-icon="fa-angle-down" data-inactive-icon="fa-angle-right">
  <div class="accordion-item bg-white active-accordion">
    <div class="accordion-header">
      <a href="#" data-bs-toggle="collapse" data-bs-target="#accordion-style-01-01" aria-expanded="true" data-bs-parent="#accordion-style-01">
        <div class="accordion-title position-relative fs-18 d-flex align-items-center pe-20px text-dark-gray fw-600 alt-font mb-0">
          Unique and bold website design<span><i class="fa-solid fa-angle-down icon-small"></i></span>
        </div>
      </a>
    </div>
    <div id="accordion-style-01-01" class="accordion-collapse collapse show" data-bs-parent="#accordion-style-01">
      <div class="accordion-body last-paragraph-no-margin">
        <p>We deliver customized marketing campaign to make a positive move.</p>
      </div>
    </div>
  </div>
  <div class="accordion-item bg-white">
    <div class="accordion-header">
      <a href="#" data-bs-toggle="collapse" data-bs-target="#accordion-style-01-02" aria-expanded="false" data-bs-parent="#accordion-style-01">
        <div class="accordion-title position-relative fs-18 d-flex align-items-center pe-20px text-dark-gray fw-600 alt-font mb-0">
          We're ready to start now<span><i class="fa-solid fa-angle-right icon-small"></i></span>
        </div>
      </a>
    </div>
    <div id="accordion-style-01-02" class="accordion-collapse collapse" data-bs-parent="#accordion-style-01">
      <div class="accordion-body last-paragraph-no-margin">
        <p>Answer content here.</p>
      </div>
    </div>
  </div>
</div>
"""
            }),

        ["process-step"] = new(
            Name: "Process Steps",
            File: "element-process-step.html",
            Styles: ["process-step-style-01", "process-step-style-02", "process-step-style-03", "process-step-style-04", "process-step-style-05"],
            KeyClasses: ["process-step-icon-box", "process-step-icon", "progress-step-separator", "box-overlay", "number"],
            Category: "Content",
            Description: "Numbered circle steps with connector lines; great for how-it-works sections.",
            Snippets: new Dictionary<string, string>
            {
                ["style-01"] = """
<div class="col text-center last-paragraph-no-margin hover-box process-step-style-01 sm-mb-40px">
  <div class="process-step-icon-box position-relative mb-25px">
    <span class="progress-step-separator bg-white w-60 separator-line-1px opacity-2"></span>
    <div class="process-step-icon d-flex justify-content-center align-items-center mx-auto rounded-circle h-80px w-80px fs-20 bg-white box-shadow-large text-dark-gray fw-500">
      <span class="fw-600 number position-relative z-index-1">01</span>
      <div class="box-overlay bg-base-color rounded-circle"></div>
    </div>
  </div>
  <span class="d-inline-block fs-17 fw-500 text-white mb-5px">Choose a hosting</span>
  <p class="w-80 md-w-90 d-inline-block">Lorem ipsum is simply dummy text.</p>
</div>
"""
            }),

        ["clients"] = new(
            Name: "Clients / Logo Carousel",
            File: "element-clients.html",
            Styles: ["clients-style-01", "clients-style-02", "clients-style-03", "clients-style-04", "clients-style-05", "clients-style-06", "clients-style-07", "clients-style-08"],
            KeyClasses: ["client-image", "transition-inner-all", "swiper", "swiper-slide", "absolute-middle-center"],
            Category: "Social Proof",
            Description: "Client/partner logo grids and carousels; Swiper-based auto-rotating.",
            Snippets: new Dictionary<string, string>
            {
                ["style-01-swiper"] = """
<div class="swiper slider-four-slide clients-style-01 magic-cursor light" data-slider-options='{ "slidesPerView": 1, "spaceBetween": 30, "loop": true, "autoplay": { "delay": 3000, "disableOnInteraction": false }, "breakpoints": { "1200": { "slidesPerView": 4 }, "992": { "slidesPerView": 3 }, "768": { "slidesPerView": 2 } }, "effect": "slide" }'>
  <div class="swiper-wrapper">
    <div class="swiper-slide">
      <div class="position-relative overflow-hidden client-image transition-inner-all">
        <img src="https://placehold.co/600x487" alt="" />
        <div class="opacity-full bg-dark-slate-blue"></div>
        <div class="absolute-middle-center"><a href="#"><img src="https://placehold.co/100x100" alt="" /></a></div>
      </div>
    </div>
  </div>
</div>
"""
            }),

        ["subscribe"] = new(
            Name: "Subscribe / Newsletter",
            File: "element-subscribe.html",
            Styles: ["newsletter-style-01", "newsletter-style-02", "newsletter-style-03"],
            KeyClasses: ["newsletter-style-01", "input-large", "form-control", "form-results"],
            Category: "Forms",
            Description: "Inline email subscription forms with various button/input layouts.",
            Snippets: new Dictionary<string, string>
            {
                ["style-01"] = """
<div class="d-inline-block w-100 newsletter-style-01 position-relative box-shadow mb-4">
  <form action="email-templates/subscribe-newsletter.php" method="post">
    <input class="input-large bg-white border-color-white form-control required" type="email" name="email" placeholder="Enter your email address" />
    <input type="hidden" name="redirect" value="" />
    <button type="submit" class="btn btn-large btn-gradient-sky-blue-pink submit" aria-label="submit">Subscribe</button>
    <div class="form-results border-radius-4px mt-15px pt-10px pb-10px ps-15px pe-15px fs-15 w-100 text-center position-absolute d-none"></div>
  </form>
</div>
"""
            }),

        ["contact-form"] = new(
            Name: "Contact Form",
            File: "element-contact-form.html",
            Styles: ["contact-form-style-01", "contact-form-style-02"],
            KeyClasses: ["form-group", "form-icon", "form-control", "form-results", "g-recaptcha"],
            Category: "Forms",
            Description: "Contact forms with icon-prefixed inputs, textarea, submit button.",
            Snippets: new Dictionary<string, string>
            {
                ["style-01"] = """
<div class="contact-form-style-01">
  <form action="email-templates/contact-form.php" method="post">
    <div class="position-relative form-group mb-20px">
      <span class="form-icon"><i class="bi bi-emoji-smile"></i></span>
      <input type="text" name="name" class="form-control required" placeholder="Your name*" />
    </div>
    <div class="position-relative form-group mb-20px">
      <span class="form-icon"><i class="bi bi-envelope"></i></span>
      <input type="email" name="email" class="form-control required" placeholder="Your email address*" />
    </div>
    <div class="position-relative form-group form-textarea">
      <span class="form-icon"><i class="bi bi-chat-square-dots"></i></span>
      <textarea placeholder="Your message" name="comment" class="form-control" rows="3"></textarea>
      <button class="btn btn-small btn-round-edge btn-dark-gray btn-box-shadow mt-20px submit" type="submit">Send message</button>
      <div class="form-results mt-20px d-none"></div>
    </div>
  </form>
</div>
"""
            }),

        ["interactive-banners"] = new(
            Name: "Interactive Banners",
            File: "element-interactive-banners.html",
            Styles: ["interactive-banner-style-01", "interactive-banner-style-02", "interactive-banner-style-03", "interactive-banner-style-04", "interactive-banner-style-05", "interactive-banner-style-06", "interactive-banner-style-07", "interactive-banner-style-08", "interactive-banner-style-09", "interactive-banner-style-10", "interactive-banner-style-11", "interactive-banner-style-12"],
            KeyClasses: ["hover-box", "bg-gradient-dark-transparent", "box-overlay"],
            Category: "Media",
            Description: "Image banners with hover overlay, icon buttons, gradient overlays.",
            Snippets: new Dictionary<string, string>
            {
                ["style-01"] = """
<div class="col text-center interactive-banner-style-01 lg-mb-30px">
  <figure class="m-0 position-relative hover-box border-radius-6px overflow-hidden">
    <img src="https://placehold.co/600x600" alt="" />
    <div class="position-absolute top-0px left-0px w-100 h-100 bg-gradient-dark-transparent opacity-5"></div>
    <figcaption class="w-100 h-100 d-flex flex-column justify-content-end align-items-center p-30px">
      <div class="position-relative z-index-1">
        <a href="#" class="d-flex justify-content-center align-items-center mx-auto icon-box w-70px h-70px rounded-circle bg-white mb-50px box-shadow-quadruple-large"><i class="bi bi-arrow-right-short text-dark-gray icon-medium lh-0px"></i></a>
        <a href="#" class="alt-font fs-18 fw-500 text-white d-block text-uppercase">Label Text</a>
      </div>
      <div class="box-overlay bg-dark-gray"></div>
    </figcaption>
  </figure>
</div>
"""
            }),

        ["review"] = new(
            Name: "Review",
            File: "element-review.html",
            Styles: ["review-style-01", "review-style-02", "review-style-03", "review-style-04", "review-style-05", "review-style-06", "review-style-07"],
            KeyClasses: ["box-shadow-quadruple-large", "box-shadow-quadruple-large-hover", "border-radius-6px", "bg-golden-yellow", "bi-star-fill"],
            Category: "Social Proof",
            Description: "Review cards with star ratings, customer images and quotes.",
            Snippets: new Dictionary<string, string>
            {
                ["style-01"] = """
<div class="col review-style-01 md-mb-30px">
  <div class="d-flex flex-column text-center box-shadow-quadruple-large box-shadow-quadruple-large-hover border-radius-6px bg-white overflow-hidden">
    <div class="position-relative">
      <img src="https://placehold.co/600x450" alt="" />
      <div class="text-center bg-golden-yellow text-white fs-15 lh-32 border-radius-22px d-inline-block ps-15px pe-15px position-absolute right-20px top-20px">
        <i class="bi bi-star-fill"></i><i class="bi bi-star-fill"></i><i class="bi bi-star-fill"></i><i class="bi bi-star-fill"></i><i class="bi bi-star-fill"></i>
      </div>
    </div>
    <div class="ps-50px pe-50px pt-40px pb-35px lg-p-30px last-paragraph-no-margin">
      <p>I wanted to hire the best and after looking at several companies i knew jacob was the perfect.</p>
    </div>
    <div class="border-top border-color-extra-medium-gray p-15px">
      <span class="alt-font text-dark-gray fs-15 text-uppercase fw-700">Jacob kalling <span class="text-medium-gray fw-600">- Walmart</span></span>
    </div>
  </div>
</div>
"""
            }),

        ["services-box"] = new(
            Name: "Services Box",
            File: "element-services-box.html",
            Styles: ["services-box-style-01", "services-box-style-02", "services-box-style-03", "services-box-style-04", "services-box-style-05"],
            KeyClasses: ["box-shadow-extra-large", "hover-box", "last-paragraph-no-margin", "border-radius-4px", "box-image"],
            Category: "Content",
            Description: "Service cards with image, description & pricing footer.",
            Snippets: new Dictionary<string, string>
            {
                ["style-01"] = """
<div class="box-shadow-extra-large services-box-style-01 hover-box last-paragraph-no-margin border-radius-4px overflow-hidden">
  <div class="position-relative box-image"><img src="https://placehold.co/600x450" alt="" /></div>
  <div class="bg-white">
    <div class="ps-50px pe-50px pt-35px sm-p-35px sm-pb-0">
      <a href="#" class="d-inline-block fs-19 primary-font fw-600 text-dark-gray mb-5px">Service title</a>
      <p>Lorem ipsum dolor eiusmod adipiscing lit tempor.</p>
    </div>
    <div class="border-top border-color-extra-medium-gray pt-20px pb-20px ps-50px pe-50px mt-30px position-relative">
      <div class="fs-17"><span class="text-dark-gray alt-font fs-26 fw-600 me-5px">$220</span>Per month</div>
      <a href="#" class="d-flex justify-content-center align-items-center w-55px h-55px lh-55 rounded-circle bg-dark-gray position-absolute right-40px top-minus-30px"><i class="bi bi-arrow-right-short text-white icon-very-medium"></i></a>
    </div>
  </div>
</div>
"""
            }),

        ["call-to-action"] = new(
            Name: "Call to Action",
            File: "element-call-to-action.html",
            Styles: [],
            KeyClasses: ["cover-background", "one-third-screen", "opacity-medium", "bg-dark-slate-blue", "fancy-text", "shape-image-animation"],
            Category: "Hero",
            Description: "Hero CTA sections with background image, overlay, animated text & gradient buttons.",
            Snippets: new Dictionary<string, string>
            {
                ["hero-cta"] = """
<section class="cover-background one-third-screen" style="background-image:url('https://placehold.co/1920x760');">
  <div class="opacity-medium bg-dark-slate-blue"></div>
  <div class="container h-100">
    <div class="row align-items-center justify-content-center h-100">
      <div class="col-xxl-8 col-lg-10 position-relative z-index-1 text-center d-flex flex-wrap align-items-center justify-content-center">
        <span class="ps-25px pe-25px pt-5px pb-5px mb-25px text-uppercase text-white fs-12 ls-1px fw-600 border-radius-100px bg-black-transparent-medium d-flex align-items-center">
          <i class="bi bi-megaphone text-white icon-small me-10px"></i> Let's make something great.
        </span>
        <h1 class="text-white fw-600 ls-minus-2px mb-45px">
          We make creative solutions for
          <span class="fw-600" data-fancy-text='{ "effect": "rotate", "string": ["business!", "brands!"] }'></span>
        </h1>
        <a href="#" class="btn btn-extra-large btn-switch-text btn-gradient-pink-orange btn-rounded">
          <span><span class="btn-double-text" data-text="Got a project in mind">Got a project in mind</span><span><i class="fa-solid fa-arrow-right"></i></span></span>
        </a>
      </div>
    </div>
  </div>
</section>
"""
            }),

        ["fancy-heading"] = new(
            Name: "Fancy Heading",
            File: "element-fancy-heading.html",
            Styles: [],
            KeyClasses: ["data-fancy-text", "image-mask", "text-gradient-*", "fancy-text-style-4", "text-outline"],
            Category: "Typography",
            Description: "Animated headings with blur-in, rotate, wave, fade, slide effects via data-fancy-text JSON.",
            Snippets: new Dictionary<string, string>
            {
                ["blur-in"] = """
<h2 class="fs-225 lg-fs-180 md-fs-150 xs-fs-90 fw-700 mb-0 text-dark-gray ls-minus-3px" data-fancy-text='{ "opacity": [0, 1], "translateY": [50, 0], "filter": ["blur(20px)", "blur(0px)"], "string": ["Animated headings"], "duration": 400, "delay": 0, "speed": 50, "easing": "easeOutQuad" }'></h2>
""",
                ["rotating-words"] = """
<span class="fancy-text-style-4">
  <span class="fs-130 xl-fs-110 lg-fs-90 md-fs-80 xs-fs-60 fw-300 text-dark-gray ls-minus-4px d-block">
    Great design made
    <span class="fw-600" data-fancy-text='{ "effect": "rotate", "string": ["affordable", "simple", "creative"], "duration": 2500 }'></span>
    for you.
  </span>
</span>
""",
                ["char-animation"] = """
<h2 class="fs-150 xl-fs-130 lg-fs-110 md-fs-90 xs-fs-65 fw-600 ls-minus-5px text-majorelle-blue mb-0">
  <span data-fancy-text='{ "opacity": [0, 1], "rotate": [10, 0], "translateX": [-30, 0], "translateY": [30, 0], "delay": 100, "speed": 50, "string": ["Animate every single character."], "easing": "easeOutQuad" }'></span>
</h2>
"""
            }),

        ["image-gallery"] = new(
            Name: "Image Gallery (Lightbox)",
            File: "element-image-gallery.html",
            Styles: ["image-gallery-style-01"],
            KeyClasses: ["gallery-wrapper", "grid", "grid-3col", "gallery-box", "gallery-image", "gallery-hover", "move-bottom-top", "lightbox-gallery"],
            Category: "Media",
            Description: "Masonry/grid image gallery with lightbox popup on click.",
            Snippets: new Dictionary<string, string>
            {
                ["grid-item"] = """
<ul class="image-gallery-style-01 gallery-wrapper grid grid-3col xl-grid-3col lg-grid-3col md-grid-2col sm-grid-1col xs-grid-1col gutter-large">
  <li class="grid-sizer"></li>
  <li class="grid-item transition-inner-all">
    <div class="gallery-box">
      <a href="https://placehold.co/785x785" data-group="lightbox-gallery" title="Gallery image title">
        <div class="position-relative gallery-image bg-white overflow-hidden">
          <img src="https://placehold.co/785x785" alt="" />
          <div class="d-flex align-items-center justify-content-center position-absolute top-0px left-0px w-100 h-100 gallery-hover move-bottom-top">
            <div class="d-flex align-items-center justify-content-center w-50px h-50px rounded-circle bg-dark-gray">
              <i class="feather icon-feather-search text-white icon-small"></i>
            </div>
          </div>
        </div>
      </a>
    </div>
  </li>
</ul>
"""
            }),

        ["marquee"] = new(
            Name: "Marquee",
            File: "element-marquee.html",
            Styles: [],
            KeyClasses: ["swiper", "swiper-width-auto", "marquee-slide", "word-break-normal"],
            Category: "Typography",
            Description: "Continuous auto-scrolling text marquee using Swiper with allowTouchMove=false.",
            Snippets: new Dictionary<string, string>
            {
                ["basic"] = """
<div class="swiper swiper-width-auto text-center" data-slider-options='{ "slidesPerView": "auto", "spaceBetween": 80, "speed": 8000, "loop": true, "allowTouchMove": false, "autoplay": { "delay": 0, "disableOnInteraction": false }, "effect": "slide" }'>
  <div class="swiper-wrapper marquee-slide">
    <div class="swiper-slide"><div class="fs-170 sm-fs-150 text-dark-gray fw-600 ls-minus-8px word-break-normal">developers</div></div>
    <div class="swiper-slide"><div class="fs-200 sm-fs-150 text-dark-gray fw-600 ls-minus-8px word-break-normal">designers</div></div>
    <div class="swiper-slide"><div class="fs-150 sm-fs-150 text-dark-gray fw-600 ls-minus-8px word-break-normal">thinkers</div></div>
  </div>
</div>
"""
            }),

        ["progress-bar"] = new(
            Name: "Progress Bar",
            File: "element-progress-bar.html",
            Styles: ["progress-bar-style-01", "progress-bar-style-02", "progress-bar-style-03", "progress-bar-style-04"],
            KeyClasses: ["progress", "progress-bar", "progress-bar-title", "progress-bar-percent"],
            Category: "Data Display",
            Description: "Horizontal skill/progress bars with percentage labels.",
            Snippets: new Dictionary<string, string>
            {
                ["style-01"] = """
<div class="progress-bar-style-01 d-block">
  <div class="progress mb-12 sm-mb-20 bg-transparent">
    <div class="progress-bar-title d-inline-block text-black text-uppercase fs-14 fw-600">Web design</div>
    <div class="progress-bar bg-black" role="progressbar" aria-valuenow="80" aria-valuemin="0" aria-valuemax="100" aria-label="web-design">
      <span class="progress-bar-percent text-center bg-black fs-11 lh-12 text-white">80%</span>
    </div>
  </div>
</div>
"""
            }),

        ["dividers"] = new(
            Name: "Dividers",
            File: "element-dividers.html",
            Styles: ["divider-style-03-01", "divider-style-03-02", "divider-style-03-03", "divider-style-03-04"],
            KeyClasses: ["divider-style-03", "border-color-dark-gray", "separator-line-1px"],
            Category: "Layout",
            Description: "Section dividers: solid, dashed, double, dotted variants.",
            Snippets: new Dictionary<string, string>
            {
                ["solid"] = """<div class="divider-style-03 divider-style-03-01 border-color-dark-gray mb-20px mt-20px w-100"></div>""",
                ["dashed"] = """<div class="divider-style-03 divider-style-03-02 border-color-dark-gray mb-20px mt-20px w-100"></div>""",
                ["double"] = """<div class="divider-style-03 divider-style-03-03 border-color-dark-gray mb-20px mt-20px w-100"></div>""",
                ["dotted"] = """<div class="divider-style-03 divider-style-03-04 border-1 border-color-dark-gray mb-20px mt-20px w-100"></div>"""
            }),

        ["social-icons"] = new(
            Name: "Social Icons",
            File: "element-social-icons.html",
            Styles: ["social-icon-style-01", "social-icon-style-02", "social-icon-style-03", "social-icon-style-04", "social-icon-style-05", "social-icon-style-06", "social-icon-style-07", "social-icon-style-08", "social-icon-style-09", "social-icon-style-10"],
            KeyClasses: ["elements-social", "extra-large-icon", "icon-with-animation", "light"],
            Category: "Social",
            Description: "Social media icon sets with various hover animations and sizes.",
            Snippets: new Dictionary<string, string>
            {
                ["style-01"] = """
<div class="elements-social social-icon-style-01">
  <ul class="extra-large-icon">
    <li><a class="facebook" href="https://www.facebook.com/" target="_blank"><i class="fa-brands fa-facebook-f"></i></a></li>
    <li><a class="twitter" href="http://www.twitter.com" target="_blank"><i class="fa-brands fa-twitter"></i></a></li>
    <li><a class="dribbble" href="http://www.dribbble.com" target="_blank"><i class="fa-brands fa-dribbble"></i></a></li>
    <li><a class="linkedin" href="http://www.linkedin.com" target="_blank"><i class="fa-brands fa-linkedin-in"></i></a></li>
    <li><a class="instagram" href="http://www.instagram.com" target="_blank"><i class="fa-brands fa-instagram"></i></a></li>
  </ul>
</div>
"""
            }),

        ["event"] = new(
            Name: "Event",
            File: "element-event.html",
            Styles: ["event-style-01", "event-style-02", "event-style-03", "event-style-04"],
            KeyClasses: ["hover-box", "dark-hover", "feature-box", "feature-box-content", "feature-box-overlay", "text-outline", "divider-style-03"],
            Category: "Content",
            Description: "Conference/event schedule cards with date, speaker, time slots and hover overlay.",
            Snippets: new Dictionary<string, string>
            {
                ["style-01"] = """
<div class="col-xl-3 col-md-4 event-style-01 p-0">
  <div class="bg-black hover-box will-change-inherit dark-hover feature-box ps-19 pe-19 pt-22 pb-27 md-p-8 overflow-hidden h-100 text-center text-md-start border-top border-end border-color-transparent-white-very-light">
    <div class="feature-box-content w-100 last-paragraph-no-margin">
      <div class="text-white fs-22 alt-font fw-500 mb-20px">Friday, Dec 24</div>
      <p class="text-white opacity-7">Psychologist - John parker<br />10:00 AM to 12:30 PM</p>
      <div class="divider-style-03 mb-20px divider-style-03-01 border-color-transparent-white-very-light"></div>
      <p class="text-white opacity-7">Sociology - Herman miller<br />02:00 PM to 04:30 PM</p>
    </div>
    <div class="feature-box-overlay bg-red"></div>
  </div>
</div>
"""
            }),

        ["rotate-box"] = new(
            Name: "Rotate Box",
            File: "element-rotate-box.html",
            Styles: ["rotate-box-style-01", "rotate-box-style-02"],
            KeyClasses: ["rotate-box", "front-side", "back-side", "to-left", "to-right", "to-top", "to-bottom"],
            Category: "Interactive",
            Description: "3D flip cards with front and back faces; directional rotation on hover.",
            Snippets: []),

        ["sliding-box"] = new(
            Name: "Sliding Box",
            File: "element-sliding-box.html",
            Styles: ["sliding-box-style-01", "sliding-box-style-02", "sliding-box-style-03"],
            KeyClasses: ["sliding-box", "sliding-box-content", "sliding-box-img"],
            Category: "Interactive",
            Description: "Content boxes that slide to reveal hidden content on hover.",
            Snippets: []),

        ["blockquote"] = new(
            Name: "Blockquote",
            File: "element-blockquote.html",
            Styles: ["blockquote-style-01", "blockquote-style-02", "blockquote-style-03", "blockquote-style-04"],
            KeyClasses: ["blockquote", "border-color-base-color", "icon-large"],
            Category: "Typography",
            Description: "Styled blockquotes with left border, icon, and attribution.",
            Snippets: []),

        ["lists"] = new(
            Name: "Lists",
            File: "element-lists.html",
            Styles: ["list-style-01", "list-style-02", "list-style-03", "list-style-04"],
            KeyClasses: ["list-style-01", "list-style-02", "list-style-03", "list-style-04"],
            Category: "Typography",
            Description: "Styled list items with borders, icons, custom markers.",
            Snippets: []),

        ["countdown"] = new(
            Name: "Countdown",
            File: "element-countdown.html",
            Styles: [],
            KeyClasses: ["countdown", "countdown-style-01"],
            Category: "Data Display",
            Description: "Countdown timer to a target date with days/hours/minutes/seconds.",
            Snippets: []),

        ["banners"] = new(
            Name: "Banners",
            File: "element-banners.html",
            Styles: ["banner-style-08"],
            KeyClasses: ["banner", "overflow-hidden", "border-radius-6px"],
            Category: "Media",
            Description: "Promotional banner cards with hover effects.",
            Snippets: []),

        ["fancy-images"] = new(
            Name: "Fancy Images",
            File: "element-fancy-images.html",
            Styles: [],
            KeyClasses: ["atropos", "parallax-effect", "tilt-box"],
            Category: "Media",
            Description: "Images with parallax, tilt, overlap, and animated reveal effects.",
            Snippets: []),

        ["horizontal-list-item"] = new(
            Name: "Horizontal List Item",
            File: "element-horizontal-list-item.html",
            Styles: [],
            KeyClasses: ["horizontal-list-item"],
            Category: "Content",
            Description: "Horizontal card layout with number, image, title and description.",
            Snippets: []),

        ["content-carousel"] = new(
            Name: "Content Carousel",
            File: "element-content-carousel.html",
            Styles: [],
            KeyClasses: ["swiper", "content-carousel"],
            Category: "Layout",
            Description: "Swiper carousel for mixed content cards (not just images).",
            Snippets: []),

        ["page-title"] = new(
            Name: "Page Title",
            File: "page-title-center-alignment.html",
            Styles: ["center-alignment", "left-alignment", "right-alignment", "big-typography", "mini-version", "parallax-background", "gallery-background", "background-video", "separate-breadcrumbs"],
            KeyClasses: ["half-section", "page-title-center-alignment", "page-title-large", "breadcrumb", "breadcrumb-style-01", "top-space-margin"],
            Category: "Layout",
            Description: "Page title/hero sections with breadcrumb; various alignment and background options.",
            Snippets: new Dictionary<string, string>
            {
                ["center"] = """
<section class="half-section page-title-center-alignment cover-background bg-very-light-gray top-space-margin">
  <div class="container">
    <div class="row">
      <div class="col-12 text-center position-relative page-title-large">
        <h1 class="d-inline-block fw-600 ls-minus-1px text-dark-gray mb-15px">Page Title Here</h1>
      </div>
      <div class="col-12 breadcrumb breadcrumb-style-01 d-flex justify-content-center">
        <ul><li><a href="#">Home</a></li><li><a href="#">Features</a></li><li>Current Page</li></ul>
      </div>
    </div>
  </div>
</section>
"""
            })
    };

    #endregion

    #region Utility Classes Reference

    private static readonly Dictionary<string, string[]> UtilityClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        ["font-size"] = ["fs-12 through fs-350", "Responsive: lg-fs-*, md-fs-*, sm-fs-*, xs-fs-*"],
        ["font-weight"] = ["fw-300 (light)", "fw-400 (normal)", "fw-500 (medium)", "fw-600 (semibold)", "fw-700 (bold)"],
        ["text-colors"] = ["text-dark-gray", "text-base-color", "text-white", "text-medium-gray", "text-light-opacity", "text-cornflower-blue", "text-majorelle-blue", "text-golden-yellow"],
        ["backgrounds"] = ["bg-white", "bg-very-light-gray", "bg-dark-slate-blue", "bg-extra-dark-slate-blue", "bg-base-color", "bg-black", "bg-gradient-sky-blue-pink", "bg-gradient-purple-pink", "bg-gradient-dark-transparent", "bg-black-transparent-medium", "bg-solitude-blue"],
        ["spacing"] = ["p-{n}, ps-{n}px, pe-{n}px, pt-{n}px, pb-{n}px", "m-{n}, ms-{n}px, me-{n}px, mt-{n}px, mb-{n}px", "Responsive: lg-p-*, md-p-*, sm-p-*, xs-p-*"],
        ["border-radius"] = ["border-radius-4px", "border-radius-6px", "border-radius-10px", "border-radius-22px", "border-radius-100px", "rounded-circle"],
        ["box-shadow"] = ["box-shadow-large", "box-shadow-extra-large", "box-shadow-double-large", "box-shadow-quadruple-large", "box-shadow-double-large-hover", "box-shadow-quadruple-large-hover"],
        ["letter-spacing"] = ["ls-minus-1px through ls-minus-10px", "ls-1px, ls-3px"],
        ["line-height"] = ["lh-20, lh-22, lh-26, lh-30, lh-100"],
        ["width"] = ["w-60, w-80, w-90, w-100", "Responsive: xl-w-*, lg-w-*, md-w-*"],
        ["opacity"] = ["opacity-1 through opacity-9", "opacity-full, opacity-light, opacity-medium, opacity-extra-medium"],
        ["icons"] = ["Line icons: line-icon-*", "Feather: feather icon-feather-*", "Bootstrap: bi bi-*", "FontAwesome: fa-solid fa-*, fa-brands fa-*", "Themify: ti-*"],
        ["icon-sizes"] = ["icon-small", "icon-medium", "icon-large", "icon-extra-large", "icon-extra-medium", "icon-very-medium"],
        ["animations"] = ["data-anime (JSON attr)", "data-fancy-text (JSON text animation)", "data-parallax-background-ratio", "animation-float", "transition-inner-all"],
        ["grid"] = ["Bootstrap 5 grid: container, row, col-*", "row-cols-1, row-cols-sm-2, row-cols-lg-4 etc.", "gutter-large, gutter-extra-large"],
        ["typography"] = ["alt-font (secondary/heading font)", "text-uppercase, text-lowercase", "text-outline, text-outline-width-*, text-outline-color-*", "word-break-normal"],
        ["sections"] = ["cover-background", "full-screen", "half-section", "one-third-screen", "top-space-margin", "overflow-hidden"],
        ["hover"] = ["hover-box", "dark-hover", "box-hover", "box-overlay", "move-bottom-top", "transition-inner-all"]
    };

    #endregion

    #region Header Variants

    private static readonly string[] HeaderVariants =
    [
        "always-fixed", "center-logo", "center-navigation", "dark",
        "disable-fixed", "hamburger-clean", "hamburger-creative",
        "hamburger-modern", "hamburger-simple", "left-menu-modern",
        "left-menu-simple", "left-navigation", "mini",
        "mobile-menu-classic", "mobile-menu-full-screen", "mobile-menu-modern",
        "one-page-navigation", "responsive-sticky", "reverse",
        "right-navigation", "top-logo", "transparent", "white",
        "with-button", "with-push", "with-social", "with-top-bar"
    ];

    #endregion

    #region Portfolio Styles

    private static readonly string[] PortfolioStyles =
    [
        "attractive", "boxed", "classic", "clean", "creative",
        "modern", "simple", "transform"
    ];

    private static readonly string[] PortfolioLayouts =
    [
        "two-column", "three-column", "four-column", "masonry", "metro"
    ];

    #endregion

    #region Blog Styles

    private static readonly string[] BlogListStyles =
    [
        "classic", "date", "grid", "masonry", "metro",
        "modern", "only-text", "side-image", "simple", "standard"
    ];

    private static readonly string[] BlogSingleStyles =
    [
        "single-classic", "single-clean", "single-creative",
        "single-modern", "single-simple"
    ];

    private static readonly string[] BlogPostTypes =
    [
        "post-type-audio", "post-type-blockquote", "post-type-gallery",
        "post-type-slider", "post-type-standard", "post-type-video"
    ];

    #endregion

    #region Demo Themes

    private static readonly string[] DemoThemes =
    [
        "accounting", "application", "architecture", "barber", "beauty-salon",
        "blogger", "branding-agency", "business", "charity", "conference",
        "consulting", "corporate", "cryptocurrency", "data-analysis",
        "decor-store", "design-agency", "digital-agency", "ebook",
        "elder-care", "elearning", "fashion-store", "finance", "freelancer",
        "green-energy", "gym-and-fitness", "horizontal-portfolio", "hosting",
        "hotel-and-resort", "interactive-portfolio", "it-business",
        "jewellery-store", "lawyer", "logistics", "magazine", "marketing",
        "medical", "minimal-portfolio", "music-onepage", "photography",
        "pizza-parlor", "product-showcase", "real-estate", "restaurant",
        "scattered-portfolio", "seo-agency", "spa-salon", "startup",
        "travel-agency", "vertical-portfolio", "web-agency",
        "wedding-invitation", "yoga-and-meditation"
    ];

    #endregion

    // ───────────────────────── MCP Tool Methods ─────────────────────────

    /// <summary>
    /// Lists all available Crafto component categories and their components.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetCraftoComponentCatalog),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get Crafto Component Catalog"),
    Description("Returns the full catalog of Crafto HTML components grouped by category, with style variants and key CSS classes for each. Use this to discover available components before requesting snippets.")]
    public static string GetCraftoComponentCatalog(
        IOptions<BaselineMCPConfiguration> options,
        [Description("Filter by category (e.g., 'Content', 'Interactive', 'Forms', 'Hero', 'Layout', 'Media', 'Social Proof', 'Typography', 'Data Display', 'Commerce', 'Social', 'Navigation'). Leave empty for all.")] string? category = null)
    {
        var filtered = string.IsNullOrWhiteSpace(category)
            ? Components
            : Components.Where(c => c.Value.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(c => c.Key, c => c.Value, StringComparer.OrdinalIgnoreCase);

        var grouped = filtered
            .GroupBy(c => c.Value.Category)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Category = g.Key,
                Components = g.Select(c => new
                {
                    Id = c.Key,
                    c.Value.Name,
                    c.Value.File,
                    c.Value.Description,
                    StyleCount = c.Value.Styles.Length,
                    c.Value.Styles,
                    c.Value.KeyClasses,
                    SnippetKeys = c.Value.Snippets.Keys.ToArray()
                }).ToArray()
            });

        var result = new
        {
            TotalComponents = filtered.Count,
            Categories = grouped
        };

        return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
    }

    /// <summary>
    /// Returns HTML snippet(s) for a specific Crafto component and variant.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetCraftoComponentSnippet),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get Crafto Component Snippet"),
    Description("Returns ready-to-use HTML snippets for a specific Crafto component. Provide the component ID (e.g., 'buttons', 'testimonials', 'accordion') and optionally a variant key (e.g., 'style-01', 'switch-text'). Returns all snippets for the component if no variant specified.")]
    public static string GetCraftoComponentSnippet(
        IOptions<BaselineMCPConfiguration> options,
        [Description("Component ID from the catalog (e.g., 'buttons', 'icon-with-text', 'testimonials', 'accordion', 'call-to-action')")] string componentId,
        [Description("Specific snippet variant key (e.g., 'style-01', 'switch-text', 'hero-cta'). Leave empty to get all snippets for the component.")] string? variant = null)
    {
        if (string.IsNullOrWhiteSpace(componentId))
            throw new ArgumentException("Component ID is required.", nameof(componentId));

        if (!Components.TryGetValue(componentId.Trim(), out var component))
        {
            var suggestions = Components.Keys
                .Where(k => k.Contains(componentId, StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .ToArray();

            return JsonSerializer.Serialize(new
            {
                Error = $"Component '{componentId}' not found.",
                AvailableComponents = Components.Keys.OrderBy(k => k).ToArray(),
                Suggestions = suggestions
            }, options.Value.SerializerOptions);
        }

        Dictionary<string, string> snippets;

        if (!string.IsNullOrWhiteSpace(variant))
        {
            if (component.Snippets.TryGetValue(variant.Trim(), out string? snippet))
            {
                snippets = new() { [variant.Trim()] = snippet };
            }
            else
            {
                return JsonSerializer.Serialize(new
                {
                    Error = $"Variant '{variant}' not found for component '{componentId}'.",
                    AvailableVariants = component.Snippets.Keys.ToArray(),
                    component.Styles
                }, options.Value.SerializerOptions);
            }
        }
        else
        {
            snippets = component.Snippets;
        }

        var result = new
        {
            Component = component.Name,
            component.File,
            component.Category,
            component.Description,
            component.KeyClasses,
            component.Styles,
            Snippets = snippets.Select(s => new { Variant = s.Key, Html = s.Value }).ToArray()
        };

        return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
    }

    /// <summary>
    /// Returns the Crafto CSS utility class reference.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetCraftoUtilityClasses),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get Crafto Utility Classes"),
    Description("Returns the complete Crafto CSS utility class reference organized by category (font-size, colors, spacing, shadows, icons, animations, etc). Use this when you need to know which CSS classes are available for styling Crafto components.")]
    public static string GetCraftoUtilityClasses(
        IOptions<BaselineMCPConfiguration> options,
        [Description("Filter by utility category (e.g., 'font-size', 'backgrounds', 'box-shadow', 'icons', 'animations'). Leave empty for all.")] string? category = null)
    {
        var filtered = string.IsNullOrWhiteSpace(category)
            ? UtilityClasses
            : UtilityClasses
                .Where(u => u.Key.Contains(category, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(u => u.Key, u => u.Value, StringComparer.OrdinalIgnoreCase);

        var result = new
        {
            TotalCategories = filtered.Count,
            AvailableCategories = UtilityClasses.Keys.OrderBy(k => k).ToArray(),
            Classes = filtered
        };

        return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
    }

    /// <summary>
    /// Returns available Crafto layout templates (headers, page titles, portfolios, blogs).
    /// </summary>
    [McpServerTool(
        Name = nameof(GetCraftoLayoutCatalog),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get Crafto Layout Catalog"),
    Description("Returns all Crafto layout patterns: header variants (28 types), page title styles (9 types), portfolio grids (8 styles x 5 layouts), blog list styles (10 types), blog single styles (5 types), blog post types (6 types), single project layouts (6 types), and modal types (6 types).")]
    public static string GetCraftoLayoutCatalog(
        IOptions<BaselineMCPConfiguration> options,
        [Description("Filter by layout type: 'headers', 'page-titles', 'portfolios', 'blogs', 'modals', 'single-projects'. Leave empty for all.")] string? layoutType = null)
    {
        var result = new Dictionary<string, object>();

        bool all = string.IsNullOrWhiteSpace(layoutType);

        if (all || layoutType!.Equals("headers", StringComparison.OrdinalIgnoreCase))
        {
            result["Headers"] = new
            {
                Count = HeaderVariants.Length,
                FilePattern = "header-{variant}.html",
                Variants = HeaderVariants
            };
        }

        if (all || layoutType!.Equals("page-titles", StringComparison.OrdinalIgnoreCase))
        {
            result["PageTitles"] = new
            {
                Count = 9,
                FilePattern = "page-title-{variant}.html",
                Variants = new[] { "center-alignment", "left-alignment", "right-alignment", "big-typography", "mini-version", "parallax-background", "gallery-background", "background-video", "separate-breadcrumbs" }
            };
        }

        if (all || layoutType!.Equals("portfolios", StringComparison.OrdinalIgnoreCase))
        {
            result["Portfolios"] = new
            {
                StyleCount = PortfolioStyles.Length,
                LayoutCount = PortfolioLayouts.Length,
                TotalCombinations = PortfolioStyles.Length * PortfolioLayouts.Length + 3,
                FilePattern = "portfolio-{style}-{layout}.html",
                Styles = PortfolioStyles,
                Layouts = PortfolioLayouts,
                SpecialLayouts = new[] { "justified-gallery", "parallax", "slider" }
            };
        }

        if (all || layoutType!.Equals("blogs", StringComparison.OrdinalIgnoreCase))
        {
            result["Blogs"] = new
            {
                ListStyles = new { Count = BlogListStyles.Length, FilePattern = "blog-{style}.html", Styles = BlogListStyles },
                SingleStyles = new { Count = BlogSingleStyles.Length, FilePattern = "blog-{style}.html", Styles = BlogSingleStyles },
                PostTypes = new { Count = BlogPostTypes.Length, FilePattern = "blog-{type}.html", Types = BlogPostTypes }
            };
        }

        if (all || layoutType!.Equals("modals", StringComparison.OrdinalIgnoreCase))
        {
            result["Modals"] = new
            {
                Count = 6,
                FilePattern = "modal-{type}.html",
                Types = new[] { "contact-form", "google-map", "simple", "subscription", "vimeo-video", "youtube-video" }
            };
        }

        if (all || layoutType!.Equals("single-projects", StringComparison.OrdinalIgnoreCase))
        {
            result["SingleProjects"] = new
            {
                Count = 6,
                FilePattern = "single-project-{style}.html",
                Styles = new[] { "creative", "gallery", "minimal", "modern", "simple", "slider" }
            };
        }

        return JsonSerializer.Serialize(new
        {
            AvailableTypes = new[] { "headers", "page-titles", "portfolios", "blogs", "modals", "single-projects" },
            Layouts = result
        }, options.Value.SerializerOptions);
    }

    /// <summary>
    /// Returns the list of Crafto demo themes for design inspiration.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetCraftoDemoThemes),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get Crafto Demo Themes"),
    Description("Returns all 52 Crafto demo themes (e.g., hotel-and-resort, restaurant, elearning, fashion-store, etc). Each theme has a homepage plus sub-pages. Use theme names as design inspiration references.")]
    public static string GetCraftoDemoThemes(
        IOptions<BaselineMCPConfiguration> options,
        [Description("Search keyword to filter themes (e.g., 'store', 'agency', 'portfolio'). Leave empty for all.")] string? search = null)
    {
        var filtered = string.IsNullOrWhiteSpace(search)
            ? DemoThemes
            : DemoThemes.Where(t => t.Contains(search, StringComparison.OrdinalIgnoreCase)).ToArray();

        var result = new
        {
            TotalThemes = filtered.Length,
            FilePattern = "demo-{theme}.html (homepage), demo-{theme}-{subpage}.html (inner pages)",
            Themes = filtered,
            ThemeCategories = new
            {
                Business = new[] { "accounting", "business", "consulting", "corporate", "finance", "it-business", "marketing", "startup" },
                Creative = new[] { "branding-agency", "design-agency", "digital-agency", "web-agency", "freelancer" },
                Commerce = new[] { "decor-store", "fashion-store", "jewellery-store" },
                Portfolio = new[] { "horizontal-portfolio", "interactive-portfolio", "minimal-portfolio", "scattered-portfolio", "vertical-portfolio", "photography" },
                Hospitality = new[] { "hotel-and-resort", "restaurant", "pizza-parlor", "spa-salon" },
                Health = new[] { "medical", "gym-and-fitness", "yoga-and-meditation", "beauty-salon", "elder-care" },
                Education = new[] { "elearning", "conference" },
                RealEstate = new[] { "real-estate", "architecture" },
                Media = new[] { "blogger", "magazine" },
                Other = new[] { "barber", "charity", "cryptocurrency", "data-analysis", "ebook", "green-energy", "hosting", "lawyer", "logistics", "music-onepage", "product-showcase", "seo-agency", "travel-agency", "wedding-invitation" }
            }
        };

        return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
    }

    /// <summary>
    /// Generates a section composition plan combining multiple Crafto components.
    /// </summary>
    [McpServerTool(
        Name = nameof(GenerateCraftoSectionPlan),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Generate Crafto Section Composition Plan"),
    Description("Generates a section-level HTML composition plan for a page using Crafto components. Provide a section type (hero, features, services, testimonials, pricing, cta, team, faq, contact, stats, gallery, blog-grid, clients) and it returns the recommended component combination with container/row/col structure.")]
    public static string GenerateCraftoSectionPlan(
        IOptions<BaselineMCPConfiguration> options,
        [Description("Section type: 'hero', 'features', 'services', 'testimonials', 'pricing', 'cta', 'team', 'faq', 'contact', 'stats', 'gallery', 'blog-grid', 'clients', 'process', 'event'")] string sectionType,
        [Description("Background style: 'light' (bg-very-light-gray), 'dark' (bg-extra-dark-slate-blue), 'white' (bg-white), 'image' (cover-background), 'gradient' (bg-gradient-*). Default: 'white'")] string background = "white",
        [Description("Number of columns for grid sections (2, 3, or 4). Default: 3")] int columns = 3)
    {
        if (string.IsNullOrWhiteSpace(sectionType))
            throw new ArgumentException("Section type is required.", nameof(sectionType));

        columns = Math.Clamp(columns, 2, 4);

        string bgClass = background.ToLowerInvariant() switch
        {
            "light" => "bg-very-light-gray",
            "dark" => "bg-extra-dark-slate-blue",
            "white" => "bg-white",
            "gradient" => "bg-gradient-sky-blue-pink",
            "image" => "cover-background",
            _ => "bg-white"
        };

        string textClass = background.ToLowerInvariant() is "dark" ? "text-white" : "text-dark-gray";

        var plan = sectionType.ToLowerInvariant() switch
        {
            "hero" => BuildSectionPlan("Hero Section", bgClass, textClass, [
                "Use: call-to-action or fancy-heading component",
                "Structure: <section class=\"full-screen cover-background\" style=\"background-image:url(...);\">",
                "Add: opacity-medium + bg-dark-slate-blue overlay for dark hero",
                "Add: data-fancy-text for animated heading text",
                "CTA: btn-switch-text btn-rounded btn-gradient-pink-orange",
                "Badge: border-radius-100px bg-black-transparent-medium text-uppercase text-white fs-12"
            ], ["call-to-action", "fancy-heading", "buttons"]),

            "features" => BuildSectionPlan("Features Section", bgClass, textClass, [
                $"Use: icon-with-text component (style-01 to style-10)",
                $"Container: row row-cols-1 row-cols-lg-{columns} row-cols-md-2",
                "Heading: centered h2 with span badge above",
                "Each feature: icon + title + short description",
                "Spacing: mb-50px sm-mb-40px per item"
            ], ["icon-with-text", "buttons"]),

            "services" => BuildSectionPlan("Services Section", bgClass, textClass, [
                "Use: services-box component (style-01 to style-05)",
                $"Container: row row-cols-1 row-cols-lg-{columns} row-cols-md-2 gutter-large",
                "Each box: image + title + description + pricing footer",
                "Add: box-shadow-extra-large border-radius-4px overflow-hidden"
            ], ["services-box", "buttons"]),

            "testimonials" => BuildSectionPlan("Testimonials Section", bgClass, textClass, [
                "Use: testimonials component (style-01 to style-06) in Swiper carousel",
                "Wrap in: swiper with data-slider-options for slidesPerView breakpoints",
                "Add: swiper-pagination below carousel",
                "Each slide: quote + avatar + name + role"
            ], ["testimonials", "carousel", "clients"]),

            "pricing" => BuildSectionPlan("Pricing Section", bgClass, textClass, [
                "Use: pricing-table component (style-01 to style-04)",
                "Container: row row-cols-1 row-cols-lg-3 row-cols-md-2",
                "Highlighted tier: add popular-item class",
                "Each card: header (plan+price) + body (feature list) + footer (CTA button)"
            ], ["pricing-table", "buttons", "lists"]),

            "cta" => BuildSectionPlan("Call to Action Section", bgClass, textClass, [
                "Use: call-to-action component",
                "Background: cover-background with opacity overlay",
                "Content: centered heading + subtext + 1-2 buttons",
                "Buttons: btn-extra-large btn-switch-text btn-rounded",
                "Optional: data-fancy-text rotating word animation"
            ], ["call-to-action", "buttons", "fancy-heading"]),

            "team" => BuildSectionPlan("Team Section", bgClass, textClass, [
                "Use: team component (style-01 to style-05)",
                $"Container: row row-cols-1 row-cols-lg-{columns} row-cols-md-2",
                "Each card: photo + name + role + hover social links",
                "Social: social-icon-style-05 with Fb. In. Tw. links"
            ], ["team", "social-icons"]),

            "faq" => BuildSectionPlan("FAQ Section", bgClass, textClass, [
                "Use: accordion component (style-01 to style-06)",
                "Layout: 2-column with image/illustration on one side",
                "First item: active-accordion class + collapse show",
                "Icons: data-active-icon=\"fa-angle-down\" data-inactive-icon=\"fa-angle-right\""
            ], ["accordion"]),

            "contact" => BuildSectionPlan("Contact Section", bgClass, textClass, [
                "Use: contact-form component (style-01 or style-02)",
                "Layout: 2-column — info/map left, form right",
                "Form fields: name, email, textarea + submit button",
                "Optional: google-map embed alongside"
            ], ["contact-form", "social-icons"]),

            "stats" => BuildSectionPlan("Stats/Counter Section", bgClass, textClass, [
                "Use: counters component (style-01 to style-05)",
                "Container: row row-cols-1 row-cols-sm-2 row-cols-lg-4",
                "Each counter: icon + animated number + label",
                "Attrs: data-speed=\"2000\" data-to=\"{number}\"",
                "Optional: progress-bar below for skill sections"
            ], ["counters", "progress-bar"]),

            "gallery" => BuildSectionPlan("Gallery Section", bgClass, textClass, [
                "Use: image-gallery component with lightbox",
                $"Grid: grid grid-{columns}col with gutter-large",
                "Each item: gallery-box with gallery-hover move-bottom-top",
                "Lightbox: data-group=\"lightbox-gallery\" on <a> tags",
                "Use grid-item-double class for 2x wide featured items"
            ], ["image-gallery"]),

            "blog-grid" => BuildSectionPlan("Blog Grid Section", bgClass, textClass, [
                "Use: blog-grid layout pattern from blog-grid.html",
                $"Container: row row-cols-1 row-cols-lg-{columns} row-cols-md-2",
                "Each card: image + category tag + title + excerpt + read more link",
                "Add: box-shadow-quadruple-large border-radius-6px overflow-hidden"
            ], ["interactive-banners", "buttons"]),

            "clients" => BuildSectionPlan("Clients/Partners Section", bgClass, textClass, [
                "Use: clients component (style-01 to style-08)",
                "Swiper carousel with autoplay for logo rotation",
                "data-slider-options: slidesPerView breakpoints for responsive",
                "Optional: row grid for static logo display"
            ], ["clients"]),

            "process" => BuildSectionPlan("Process Steps Section", bgClass, textClass, [
                "Use: process-step component (style-01 to style-05)",
                "Container: row row-cols-1 row-cols-lg-4 row-cols-md-2",
                "Each step: numbered circle + connector line + title + description",
                "Connector: progress-step-separator between circles"
            ], ["process-step"]),

            "event" => BuildSectionPlan("Event Schedule Section", bgClass, textClass, [
                "Use: event component (style-01 to style-04)",
                "Container: row with col-xl-3 col-md-4 per card",
                "Each card: date + speaker sessions + time slots",
                "Dark theme: bg-black with text-white and feature-box-overlay"
            ], ["event"]),

            _ => new
            {
                Error = $"Unknown section type '{sectionType}'.",
                AvailableTypes = new[] { "hero", "features", "services", "testimonials", "pricing", "cta", "team", "faq", "contact", "stats", "gallery", "blog-grid", "clients", "process", "event" }
            } as object
        };

        return JsonSerializer.Serialize(plan, options.Value.SerializerOptions);
    }

    /// <summary>
    /// Returns a full-page Crafto composition plan for a specific page type.
    /// </summary>
    [McpServerTool(
        Name = nameof(GenerateCraftoPagePlan),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Generate Crafto Full Page Plan"),
    Description("Generates a full-page composition plan listing recommended sections in order for common page types like 'homepage', 'about', 'services', 'contact', 'blog-listing', 'blog-single', 'portfolio', 'landing', 'pricing'. Returns the section order with recommended components for each.")]
    public static string GenerateCraftoPagePlan(
        IOptions<BaselineMCPConfiguration> options,
        [Description("Page type: 'homepage', 'about', 'services', 'contact', 'blog-listing', 'blog-single', 'portfolio', 'landing', 'pricing', 'event'")] string pageType,
        [Description("Demo theme for design reference (e.g., 'hotel-and-resort', 'business', 'restaurant'). Optional.")] string? demoTheme = null)
    {
        if (string.IsNullOrWhiteSpace(pageType))
            throw new ArgumentException("Page type is required.", nameof(pageType));

        var sections = pageType.ToLowerInvariant() switch
        {
            "homepage" =>
            [
                new PageSection("Hero", "Full-screen hero with animated heading and CTA button", ["call-to-action", "fancy-heading", "buttons"]),
                new PageSection("Features", "3-4 column icon-with-text feature boxes", ["icon-with-text"]),
                new PageSection("Services", "Service cards with images and pricing", ["services-box", "interactive-banners"]),
                new PageSection("Stats", "Counter section with animated numbers", ["counters"]),
                new PageSection("Testimonials", "Client quotes in Swiper carousel", ["testimonials", "carousel"]),
                new PageSection("Clients", "Logo carousel of partners/clients", ["clients"]),
                new PageSection("CTA", "Final call-to-action with background image", ["call-to-action", "buttons"]),
            ],
            "about" =>
            [
                new PageSection("Page Title", "Centered page title with breadcrumb", ["page-title"]),
                new PageSection("Story", "2-column: image left + text right with history", ["fancy-images"]),
                new PageSection("Process", "How we work steps", ["process-step"]),
                new PageSection("Team", "Team member cards", ["team", "social-icons"]),
                new PageSection("Stats", "Company statistics counters", ["counters"]),
                new PageSection("Clients", "Partner logos", ["clients"]),
            ],
            "services" =>
            [
                new PageSection("Page Title", "Page title with breadcrumb", ["page-title"]),
                new PageSection("Services Grid", "Service box cards in grid", ["services-box"]),
                new PageSection("Features", "Detailed feature list with icons", ["icon-with-text"]),
                new PageSection("Process", "How it works steps", ["process-step"]),
                new PageSection("Pricing", "Pricing table comparison", ["pricing-table"]),
                new PageSection("FAQ", "Frequently asked questions accordion", ["accordion"]),
                new PageSection("CTA", "Contact call-to-action", ["call-to-action", "buttons"]),
            ],
            "contact" =>
            [
                new PageSection("Page Title", "Page title with breadcrumb", ["page-title"]),
                new PageSection("Contact Info", "Icon-with-text cards for phone/email/address", ["icon-with-text"]),
                new PageSection("Form + Map", "2-column: form left, map right", ["contact-form"]),
                new PageSection("Social", "Social media links", ["social-icons"]),
            ],
            "blog-listing" =>
            [
                new PageSection("Page Title", "Blog title with breadcrumb", ["page-title"]),
                new PageSection("Blog Grid", "Blog cards in grid or masonry layout", []),
                new PageSection("Pagination", "Page navigation", []),
                new PageSection("Subscribe", "Newsletter subscription form", ["subscribe"]),
            ],
            "blog-single" =>
            [
                new PageSection("Page Title", "Post title with featured image", ["page-title"]),
                new PageSection("Content", "Rich text article content", ["blockquote", "lists"]),
                new PageSection("Author", "Author bio card", ["team"]),
                new PageSection("Related", "Related posts grid", []),
                new PageSection("Comments", "Comment form", ["contact-form"]),
            ],
            "portfolio" =>
            [
                new PageSection("Page Title", "Portfolio header", ["page-title"]),
                new PageSection("Filter", "Category filter tabs", ["tabs"]),
                new PageSection("Gallery", "Portfolio grid with lightbox", ["image-gallery"]),
                new PageSection("CTA", "Hire us call-to-action", ["call-to-action", "buttons"]),
            ],
            "landing" =>
            [
                new PageSection("Hero", "Full-screen hero with form or CTA", ["call-to-action", "fancy-heading"]),
                new PageSection("Features", "Key benefits icon boxes", ["icon-with-text"]),
                new PageSection("Social Proof", "Testimonials + client logos", ["testimonials", "clients"]),
                new PageSection("Pricing", "Pricing comparison", ["pricing-table"]),
                new PageSection("FAQ", "Common questions", ["accordion"]),
                new PageSection("Final CTA", "Last conversion push", ["call-to-action", "subscribe"]),
            ],
            "pricing" =>
            [
                new PageSection("Page Title", "Pricing page title", ["page-title"]),
                new PageSection("Pricing Table", "Plan comparison cards", ["pricing-table"]),
                new PageSection("Features", "Detailed feature comparison", ["tabs", "lists"]),
                new PageSection("FAQ", "Pricing FAQ", ["accordion"]),
                new PageSection("CTA", "Get started call-to-action", ["call-to-action", "buttons"]),
            ],
            "event" =>
            [
                new PageSection("Hero", "Event hero with countdown", ["call-to-action", "countdown"]),
                new PageSection("Schedule", "Event schedule cards", ["event"]),
                new PageSection("Speakers", "Speaker team cards", ["team"]),
                new PageSection("Pricing", "Ticket pricing", ["pricing-table"]),
                new PageSection("Gallery", "Event photo gallery", ["image-gallery"]),
                new PageSection("CTA", "Register now", ["call-to-action", "subscribe"]),
            ],
            _ => new[]
            {
                new PageSection("Error", $"Unknown page type '{pageType}'. Use: homepage, about, services, contact, blog-listing, blog-single, portfolio, landing, pricing, event", [])
            }
        };

        var result = new
        {
            PageType = pageType,
            DemoReference = demoTheme is not null ? $"demo-{demoTheme}.html" : null as string,
            SectionCount = sections.Length,
            Sections = sections.Select((s, i) => new
            {
                Order = i + 1,
                s.Name,
                s.Description,
                RecommendedComponents = s.Components,
                Tip = i == 0
                    ? "Use full-screen or half-section class for the first section"
                    : i % 2 == 0
                        ? "Alternate background: bg-very-light-gray"
                        : "Keep bg-white for contrast"
            }).ToArray(),
            GeneralGuidelines = new[]
            {
                "Wrap each section in <section> with appropriate padding classes (p-0 for hero, default for others)",
                "Use container > row > col grid system inside each section",
                "Alternate section backgrounds: white / very-light-gray for visual rhythm",
                "Add top-space-margin to the first section after header",
                "Use section-heading pattern: small badge + h2 + paragraph for section intros",
                "Responsive: always include lg-*, md-*, sm-*, xs-* breakpoint classes"
            }
        };

        return JsonSerializer.Serialize(result, options.Value.SerializerOptions);
    }

    // ───────────────────────── helpers ─────────────────────────

    private static object BuildSectionPlan(
        string name, string bgClass, string textClass,
        string[] guidelines, string[] components) => new
        {
            Section = name,
            BackgroundClass = bgClass,
            TextClass = textClass,
            Guidelines = guidelines,
            RecommendedComponents = components,
            StructureTemplate = $"""
<section class="{bgClass}">
  <div class="container">
    <div class="row justify-content-center mb-4">
      <div class="col-xl-7 col-lg-8 text-center">
        <span class="fs-13 text-uppercase fw-700 {textClass} d-inline-block mb-10px">Section Label</span>
        <h2 class="alt-font {textClass} fw-600 ls-minus-1px">Section Title</h2>
      </div>
    </div>
    <div class="row">
      <!-- Component content here -->
    </div>
  </div>
</section>
"""
        };

    // ───────────────────────── types ─────────────────────────

    private sealed record ComponentInfo(
        string Name,
        string File,
        string[] Styles,
        string[] KeyClasses,
        string Category,
        string Description,
        Dictionary<string, string> Snippets);

    private sealed record PageSection(
        string Name,
        string Description,
        string[] Components);
}
