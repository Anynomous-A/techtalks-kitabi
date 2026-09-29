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


. Sprint 1 Outcomes

✅ Database schema created (Users, Listings).

✅ Book posting API implemented (POST /api/books).

✅ Search API basic version implemented (GET /listings/search).

✅ WhatsApp/SMS contact endpoint added.

✅ Seeded test user (UserId = 1) for development.

✅ README updated with setup instructions.

3. Setup Instructions

How to run the app locally:

bash
dotnet run
Database setup (EF Core migrations, seeding).

Test user credentials (dummy email, phone, password hash).

4. Usage Examples

Create a book listing (form-data):

Code
POST /api/books
SellerId=1
Title=Algorithms
Author=Cormen
Price=25
Condition=Good
City=Beirut
Search listings:

Code
GET /listings/search?title=Algorithms
5. Known Limitations (Sprint 1)

No authentication yet.

No advanced search filters (coming in Sprint 2).

No frontend UI (planned for Sprint 3).

6. Next Steps

## 📅 Sprint 2 Outcomes

✅ Implemented **global input validation and sanitization middleware**  
- All JSON requests are sanitized (HTML stripped, strings trimmed).  
- Invalid payloads return structured error responses (`400 Bad Request` with JSON message + errors).  

✅ Added **authentication & security policies**  
- JWT Bearer authentication integrated into Swagger.  
- Role‑based policies applied to protected endpoints.  

✅ Seeded **5 sample users** (buyers/sellers combined)  
- Users include realistic names, emails, universities, and listings.  
- Some users act as sellers with multiple listings, others as buyers only.  
- Enables testing of both buyer and seller flows.  

✅ Enhanced **search API**  
- Listings can be queried by title, author, or category.  
- Results include seller and book details.  

---

### 🔍 How to Test Sprint 2
1. **Run the app** and open Swagger UI.  
2. **Authentication**:  
   - Use JWT Bearer tokens to access protected endpoints.  
   - Swagger now shows the `Authorize` button.  
3. **Validation**:  
   - Submit invalid payloads (e.g., missing required fields).  
   - Confirm structured error JSON is returned.  
4. **Seeding**:  
   - Query `/listings/search` to see seeded books (e.g., *Clean Code*, *Database Systems*).  
   - Test buyer/seller flows using seeded users (`nizar@example.com`, `sara@example.com`, etc.).  

---

### ⚠️ Known Limitations (Sprint 2)
- No advanced search filters yet (planned for Sprint 3).  
- No frontend UI (planned for Sprint 3).  
- File/image upload limited to basic functionality.  

---

### 🚀 Next Steps
- **Sprint 3** will deliver:  
  - Frontend integration (mobile‑first UI).  
  - Advanced search filters (by city, condition, price range).  
  - Improved listing image handling.  

