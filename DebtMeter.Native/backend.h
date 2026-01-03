#pragma once
#ifdef __cplusplus
extern "C" {
#endif

	__declspec(dllexport)
		int ProjectDebt(const double* debt, const double* rate, int n, double periodDivisor, double* outDebt);

#ifdef __cplusplus
}
#endif
