# techtalks-kitabi
A lightweight, mobile‑first web app for Lebanon that connects students, libraries, and individuals to buy and sell new or used books at affordable prices. The platform focuses on simplicity:


## 📖 Project Overview
**Problem:** Students and readers struggle with the high cost of new books. Used books are scattered across WhatsApp groups or word‑of‑mouth, wasting time and limiting access.  
**Target Users:** University students, school families, libraries, and individuals with surplus books.  
**Solution:** A simple platform to post, browse, and connect directly with sellers for cash‑on‑delivery exchanges.

---

## ✅ Approved MVP
**Core Features**
1. Post a book (title, author, condition, price, location).  
2. Browse and search listings by subject or price.  
3. Contact seller via pre‑filled WhatsApp/SMS message.  
4. Basic profiles to reduce spam and build trust.  

**Out of Scope (Planned Later)**
- Online payments (Visa, OMT, WishCard).  
- Delivery tracking/logistics.  
- Advertising or promotional dashboards.  
- Advanced analytics/recommendations.  

**Current Status:** Research validated, MVP scoped, awaiting mentor review before implementation.

---

## 🛠 Tech Stack
- **Frontend:** HTML, CSS, JavaScript (vanilla, mobile‑first).  
- **Backend:** ASP.NET Core APIs and business logic.  
- **Database:** SQL Server for listings, profiles, and transactions.  
- **External Services:** WhatsApp/SMS integration for communication.  

---

## 👥 Team & Responsibilities
- **Nizar abou ibrahim** – Team Lead, Backend/API development,Frontend/UI, Database, Research  
- **Yehya Allaw**  – Backend/API development,Frontend/UI, Database, Research
- *youseg alkadri* – Backend/API development,Frontend/UI, Database, Research
- *mahmood koros* – Backend/API development,Frontend/UI, Database, Research
- GitHub usernames: [to be added]  
- Contact: [team email or group chat link]

---

## 🚀 Getting Started
**Prerequisites**
- .NET 6+  
- SQL Server  
- Node.js (optional for tooling)  

**Setup**
```bash
# Clone repo
git clone https://github.com/your-org/techtalks-kitabi.git
cd techtalks-kitabi

# Backend setup
dotnet restore
dotnet run

# Database setup
# Create SQL Server DB and update connection string in appsettings.json
