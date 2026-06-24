# Introduction 
BRaVe 2025: This repository contains all components of the new BRaVe architecture. 
It serves as the foundation for building and deploying the next-generation BRaVe platform.

# Getting Started
This section will guide you through setting up the project on your local system.
1.	Clone the Repository
git clone https://IOMCloud@dev.azure.com/IOMCloud/BRaVeDHRR/_git/BRaVe2025
cd BRaVe2025


2.	Set Up the Database
Create a new database named BRaVe-db.
Navigate to the Sql folder.
Run the SQL scripts in the indicated order to initialize the database schema and seed data.
Run the SQL scripts in the Sql folder in the indicated order to initialize the schema and seed data.
⚠️ Make sure to follow the order specified in the folder to avoid dependency issues.

-- Example
CREATE DATABASE BRaVe-db;
-- Then run the scripts


3.	Latest releases
Ensure you have the following software versions or later installed:

SQL Server 2019 or later
Visual Studio 2022 or later
Android Studio Dolphin or later
