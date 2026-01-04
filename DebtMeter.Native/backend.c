#include "pch.h"
#include "backend.h"

int ProjectDebtAsm(const double* debt, const double* rate, int n, double periodDivisor, double* outDebt);

__declspec(dllexport)
int ProjectDebt(const double* debt, const double* rate, int n, double periodDivisor, double* outDebt)
{
    return ProjectDebtAsm(debt, rate, n, periodDivisor, outDebt);
}
