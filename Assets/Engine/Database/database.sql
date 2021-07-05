# Simple local database for SS3D
# This should carry characters, bans and admin stuff
# To make this work read this carefully, 
# and if questions persist feel free to ping SS3D CentCom members at our Discord
#
# Please remember to update the .dlls on the Database folder in case of weird errors.
# I made this work by simply searching download links for them.

# Steps to make this work
# 1) Download and install MySQL, there's a good full installer on their site.
#   Check if you're downloading everything necessary,
#   MySQL Workbench is great to have so you can check manually your data
#   TODO: download link
# 2) Create your database.
#   Open MySQL Workbench and create a new database.
# 3) Run this SQL in a database of name from your choice, 
#   because I can't figure out how to run from in-game.
#   TODO: figure out how to run from in-game.
# 4) Go to the Editor, load the Lobby scene, and update the LocalDatabaseManager game object with your data,
#   TODO: make this be in a .txt file and/or inside the game.
# 5) Make sure it connects, you can do that with the DatabaseManager object, there's a function to check that, 
#   it should be already automated and you should be able to see it in the Console view
#   (and in the future in an in-game console), double check if necessary.
# 6) Profit.

# This is the way.

CREATE DATABASE IF NOT EXISTS SS3D
DEFAULT CHARACTER SET utf8;

# Users that have joined the server
CREATE TABLE IF NOT EXISTS registeredUsers(
	ckey VARCHAR(60) PRIMARY KEY
);

# User saved characters
# ckey is for the character owner
# Note: JSON to save multiple stuff in one register
CREATE TABLE IF NOT EXISTS characterData (
    idCharacterData  INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ckey VARCHAR(60) NOT NULL,
	name VARCHAR(200),
	sex VARCHAR(20),
    jobPreferences JSON,
    
    FOREIGN KEY (ckey)
    REFERENCES registeredUsers(ckey)
)