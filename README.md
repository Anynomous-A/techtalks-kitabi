📚 Kitabi – Second‑Hand Book Marketplace
Overview
Kitabi is a web application designed to help students and readers in Lebanon exchange second‑hand books easily and affordably. The platform provides a simple marketplace where users can post listings, browse available books, and contact sellers directly via WhatsApp or SMS.

This project was developed as part of the TechTalks internship program, focusing on building a Minimum Viable Product (MVP) with core functionality, usability, and deployment readiness.

✨ Features
Book Listings: Add books with title, author, condition, price, and location.

Search & Filters: Browse listings with keyword search and category filters.

Direct Contact: WhatsApp/SMS buttons to reach sellers instantly.

User Profiles: Basic profile creation and listing management.

Responsive UI: Mobile‑first design for accessibility.

🛠️ Technologies Used
Backend: ASP.NET Minimal APIs, EF Core InMemory

Frontend: HTML, CSS, JavaScript (responsive navigation, Help page)

Database: PostgreSQL (for deployed version)

Deployment: Docker + Render

Version Control: GitHub

⚙️ Setup Instructions
Prerequisites
.NET 6+

Node.js (for frontend build tools if needed)

PostgreSQL (for production database)

Docker (optional, for containerized deployment)

Steps
Clone the repository

bash
git clone https://github.com/your-org/techtalks-kitabi.git
cd techtalks-kitabi
Backend setup

bash
cd Kitabi.Api
dotnet restore
dotnet run
Frontend setup

bash
cd Kitabi.Frontend
npm install
npm start
Environment variables  
Create a .env file with:

Code
DB_CONNECTION_STRING=your_postgres_connection
JWT_SECRET=your_secret_key
Access the app

API runs on http://localhost:5000

Frontend runs on http://localhost:3000

📐 System Architecture
Code
Frontend (React/HTML/CSS/JS) → Backend (ASP.NET Minimal APIs) → Database (PostgreSQL)
Frontend: Handles user interface and navigation.

Backend: Provides REST endpoints for listings, profiles, and search.

Database: Stores book listings and user data.

🚀 Deployment
Dockerized: Build and run with docker-compose up.

Render: Deployed version available at: [Demo URL here]

👥 Team Contributions
Backend Development: API endpoints, EF Core, deployment setup

Frontend Development: Navbar, Help page, responsive design

Database & Deployment: PostgreSQL migration, Docker setup

Documentation: README, final report, sprint notes

🧪 Testing
Manual testing of posting → browsing → contacting flow

Edge case handling for invalid inputs

Internal bug fixes during sprints

🔒 Security & Privacy
Environment variables hidden in .env

No credentials exposed in repository

User data limited to non‑sensitive fields (name, contact, book info)

📸 Screenshots
(Add screenshots of posting form, browsing page, contact button, and profile page here)

📈 Future Improvements
Secure online payments

Delivery tracking/logistics integration

Recommendation engine for books

Mobile app version
