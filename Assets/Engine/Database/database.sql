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

create table characterData (
	name varchar(200),
    gender varchar(200)
)