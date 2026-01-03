# AplDebtmeter
Project for Apl

---

## Version 0.1 Instructions

- The solution contains **2 projects**
- Make sure to build **DebtMeter.Native first**, and **DebtMeter.Gui second**

---

## Contents

### `Dir: DebtMeter.Gui\`
Front-end WinForms application that loads the CSV, passes it into the ASM backend, receives data, and draws a graph.

**Important files:**
- `DebtMeter.Gui.sln`  
  The main Visual Studio 2022 solution for this project
- `Program.cs`  
  `main()` file, function for importing the backend `.dll`
- `Form1.Designer.cs`  
  WinForms frontend design
- `Form1.cs`  
  Logic for importing CSV and calling the backend `.dll`

---

### `Dir: DebtMeter.Gui\bin\Debug\`
Working directory for the project  
(this is where the `.csv` and the `.dll` files should be)

---

### `Dir: DebtMeter.Native\`
Back-end directory with the assembler file and C files required for calling it.

**Important files:**
- `Backend.h`  
  Header declaring the ASM function
- `Backend.c`  
  C file exporting the `.dll` and calling the `.asm` function
- `compute.asm`  
  The backend logic
