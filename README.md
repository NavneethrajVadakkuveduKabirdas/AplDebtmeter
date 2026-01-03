# AplDebtmeter
project for Apl

#Version 0.1 instructions:
1 Solution 2 projects, make sure to build DebtMeter.Native first and DebtMeter.Gui second

Contents:
*Dir: DebtMeter.Gui\ > Front end winForm app that loads the csv passes it into the asm backend, recieves data and draws a graph*
  Important files:
  -DebtMeter.Gui.sln > the main visual studio 2022 solution for this project
  -Program.cs > main() file, function for importing the backend .dll     
  -Form1.Designer.cs > winform frontend design
  -Form1.cs > logic for importing csv and calling the backend .dll
  
*Dir:...\DebtMeter.Gui\bin\Debug\ > working directory for the project (this is where the .csv and the .dll files should be)*

*Dir: DebtMeter.Native\ > Back end directory with the assembler file and C files required for calling it*
  Important files: 
  -Backend.h > header declaring the asm function
  -Backend.c > C file exporting the .dll and calling the .asm function
  -compute.asm > the backend logic

