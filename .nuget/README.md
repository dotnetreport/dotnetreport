![Dotnet Report Builder](https://raw.githubusercontent.com/dotnetreport/dotnetreport/master/Content/img/report-logo.png)

# Dotnet Report Builder

**Embedded Analytics & Ad-Hoc Reporting for ASP.NET Core Applications**

[![NuGet Version](https://img.shields.io/nuget/v/dotnetreport?color=blue&label=nuget)](https://www.nuget.org/packages/dotNetReport.mvc/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/dotnetreport?color=green&label=downloads)](https://www.nuget.org/packages/dotNetReport.mvc/)
[![License](https://img.shields.io/badge/license-LGPL%20%2B%20EULA-orange)](https://github.com/dotnetreport/ReportBuilder.Web/blob/master/LICENSE)
[![Live Demo](https://img.shields.io/badge/demo-live-brightgreen)](https://dotnetreport.com/demo/Report)

---

## What is Dotnet Report?

**Dotnet Report** is a drop-in .NET NuGet package that lets you embed a full-featured, self-service report builder and analytics dashboard directly into your ASP.NET Core or MVC web application — no extra servers, no third-party BI tools, no headaches.

Give your end-users the power to build, filter, and visualize their own reports without writing a single line of SQL. Give your developers a clean, well-documented API that integrates in minutes.

---

## ✨ Features

### 📊 Report Building
- **Drag-and-drop report designer** — intuitive interface for non-technical users
- **List & Summary Reports** — flat tabular reports or grouped/aggregated summaries
- **Aggregate Functions** — COUNT, SUM, MIN, MAX, and AVERAGE
- **Sorting, Filtering & Grouping** — full control over how data is sliced and presented
- **Column Formatting** — custom display formats, conditional highlighting, and more

### 📈 Data Visualization
- **Charts** — Pie, Bar, Line, and more
- **Interactive Dashboards** — combine multiple reports and charts into a single view
- **Drill-down Support** — click into charts and summaries for row-level detail

### 🔒 Security & Access Control
- **Role-based report access** — control who can view, create, or edit reports
- **User-level data filtering** — automatically scope data to the logged-in user
- **Secure API integration** — token-based communication between your app and the report engine

### 📤 Export & Sharing
- Export reports to **PDF**, **Excel (XLSX)**, and **CSV**
- **Scheduled reports** — automatically email reports on a set cadence
- **Shareable report links** for collaboration

### 🛠 Developer Experience
- **Single NuGet package** install — no separate server or service to deploy
- **Works with any SQL database** — SQL Server, PostgreSQL, MySQL
- Compatible with **.NET 6, .NET 7, .NET 8**, and ASP.NET Core MVC
- **White-label ready** — fully customizable UI to match your brand

---

## 🚀 Getting Started

### 1. Install the NuGet Package

```bash
dotnet add package dotnetreport
```

Or via the Package Manager Console in Visual Studio:

```powershell
Install-Package dotnetreport
```

### 2. Register Your Account

Sign up for a free account at [dotnetreport.com](https://dotnetreport.com) to get your **Account API Token** and **Data Connect API Token**.

### 3. Configure Your App

Add your tokens and connection string to `web.config`:

```
<appSettings>
  <add key="dotNetReport.accountApiToken" value="your-account-token" />
  <add key="dotNetReport.dataconnectApiToken" value="your-dataconnect-token" />
</appSettings>
```

### 4. Follow the Getting Started Guide

For complete setup instructions, middleware configuration, and your first report, see the 📖 [Getting Started Guide](https://dotnetreport.com/blog/getting-started-with-dotnet-report/).

---

## 🖥 Live Demo

See Dotnet Report in action before you install anything:

👉 **[https://dotnetreport.com/demo/Report](https://dotnetreport.com/demo/Report)**

---

## 📚 Documentation

Full documentation, configuration options, API reference, and tutorials are available in our knowledge base:

👉 **[https://dotnetreport.com/kb/](https://dotnetreport.com/kb/)**

---

## 🤝 Support

- 💬 **Community & Questions** — [dotnetreport.com/contact](https://dotnetreport.com/contact)
- 🐛 **Bug Reports** — [Open a GitHub Issue](https://github.com/dotnetreport/ReportBuilder.Web/issues)
- 📧 **Enterprise Support** — contact us at [contact@dotnetreport.com](mailto:contact@dotnetreport.com)

---

## 📄 License

Dotnet Report Builder is licensed under [LGPL](https://github.com/dotnetreport/ReportBuilder.Web/blob/master/LICENSE) and a commercial [EULA](https://dotnetreport.com/eula).

---

Built with ❤️ by the [Dotnet Report](https://dotnetreport.com) team