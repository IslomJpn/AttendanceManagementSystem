# Attendance Management System

Professional web-based Attendance Management System developed using ASP.NET Core MVC, Entity Framework Core, SQL Server, and Bootstrap.

---

# Project Overview

The Attendance Management System is designed to help companies manage employee attendance efficiently.

The system provides two user roles:

- Employee
- Administrator

Employees can manage their own attendance, while administrators can monitor attendance records, manage employees, approve requests, and generate attendance summaries.

---

# Main Features

## Employee

- Login
- Dashboard
- Clock In
- Clock Out
- Attendance History
- Attendance Correction Request
- Paid Leave Request
- Paid Leave History
- Password Change

---

## Administrator

- Login
- Dashboard
- Employee Management
- Attendance Management
- Attendance Search
- Monthly Attendance Summary
- Paid Leave Management
- Attendance Correction Approval
- Paid Leave Approval
- Password Reset
- CSV Export

---

# Technology Stack

- C#
- ASP.NET Core MVC
- Entity Framework Core
- SQL Server
- Bootstrap 5
- HTML5
- CSS3
- JavaScript

---

# System Architecture

```
Presentation Layer
        │
ASP.NET Core MVC
        │
Business Logic
        │
Entity Framework Core
        │
SQL Server Database
```

---

# Project Structure

```
Controllers/
Models/
ViewModels/
Views/
Services/
Helpers/
Data/
Migrations/
Properties/
wwwroot/

Documents/
Database/
Demo/
Screenshots/
```

---

# Documents

The following project documents are included.

| Document | Format |
|----------|--------|
| Requirements Definition (要件定義書) | PDF / DOCX |
| Basic Design (基本設計書) | PDF / DOCX |
| Detailed Design (詳細設計書) | PDF / DOCX |

---

# Database

Database Backup

```
AttendanceManagementSystemFinalDb.bak
```

Restore the database using Microsoft SQL Server Management Studio (SSMS).

---

# Demo Video

Google Drive

https://drive.google.com/drive/folders/1AX95a7W2CRKgWBPT2wjm1GvNjuexaWuB

---

# Screenshots

Project screenshots are available inside the **Screenshots** folder.

Examples include:

- Login
- Employee Dashboard
- Admin Dashboard
- Attendance History
- Employee Management
- Attendance List
- Monthly Summary
- Paid Leave
- Attendance Correction

---

# Installation

1. Clone this repository

```
git clone https://github.com/IslomJpn/AttendanceManagementSystem.git
```

2. Open the solution using Visual Studio 2022

3. Restore NuGet packages

4. Restore the SQL Server database

5. Update the Connection String if necessary

6. Build the solution

7. Run the project

---

# Development Environment

- Visual Studio 2022
- .NET 8
- SQL Server
- Entity Framework Core
- Bootstrap 5

---

# Future Improvements

- Email Notifications
- Dashboard Charts
- Mobile Responsive UI Improvements
- PDF Export
- Audit Logs
- GPS Attendance
- Face Recognition Login

---

# Author

**Islombek Kamolov**

---

# License

This project was developed for educational and portfolio purposes.
