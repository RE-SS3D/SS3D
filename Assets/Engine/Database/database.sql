# Simple local database for SS3D
# This should carry characters, bans and admin stuff
# To make this work just download MySQL
# Please remember to update the .dlls on the Database folder
# Steps to make this work

# 1) Download MySQL and install MySQL Workbench
# 2) Run this SQL in a database of name from your choice, but you'll have to configure it in the game on the DatabaseManager object
# 3) Make sure it connects, you can do that with the DatabaseManager object, there's a function to check that.
# 4) Profit

# This is the way.

CREATE DATABASE IF NOT EXISTS SS3D
DEFAULT CHARACTER SET utf8;

CREATE TABLE IF NOT EXISTS registeredUsers(
	ckey VARCHAR(60) PRIMARY KEY
);

CREATE TABLE IF NOT EXISTS characterData (
    idCharacterData  INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ckey VARCHAR(60) NOT NULL	,
	name VARCHAR(200),
    jobPreferences JSON,
    
    FOREIGN KEY (ckey)
    REFERENCES registeredUsers(ckey)
)