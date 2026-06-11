# Habit Tracker

This is a small C# console application for tracking daily habits in a local SQLite database.

I built it as a CRUD practice project: the app lets you create, view, update, and delete habit records from the terminal. At the moment it supports two example habits:

- Drinking water, measured in millilitres
- Coding time, measured in hours

The main goal of the project was to get more comfortable with C#, SQLite, user input validation, and keeping a simple console program organised enough that it is easy to follow.

## How it works

When the app starts, it creates a SQLite database if one does not already exist. The database has a `habits` table for the habit definitions and a `records` table for the logged entries. Each record stores a `HabitId`, which links it back to the habit it belongs to.

From the main menu, you can choose which habit you want to work with. Each tracker then has its own menu where you can:

- View all records
- Insert a new record
- Delete an existing record
- Update an existing record
- Return to the main menu

Dates are entered in `dd-mm-yy` format, and numeric values are validated so the app does not accept negative numbers or non-number input.


## Possible improvements

Some improvements I would like to make are:

- Add the ability to create custom habits from inside the app
- Add summaries, such as totals by week or month



