; int ProjectDebt(const double* debt, const double* rate, int n, double periodDivisor, double* outDebt)
; Windows x64 calling convention:
; RCX = debt*
; RDX = rate*
; R8D = n
; XMM3 = periodDivisor (4th param is floating -> in XMM3)
; R9  = outDebt*

option casemap:none
.code

PUBLIC ProjectDebtAsm

ProjectDebtAsm PROC
    ; Return 0 on success, non-zero on error
    test rcx, rcx
    jz   fail
    test rdx, rdx
    jz   fail
    test r9,  r9
    jz   fail
    test r8d, r8d
    jle  fail

    ; xmm3 = periodDivisor
    ; compute invDiv = 1.0 / periodDivisor in xmm4
    movsd xmm4, qword ptr [OneConst]
    divsd xmm4, xmm3

    xor r10d, r10d            ; i = 0
loop_start:
    cmp r10d, r8d
    jge done

    movsxd r11, r10d

    ; xmm0 = debt[i]
    movsd xmm0, qword ptr [rcx + r11*8]

    ; xmm1 = rate[i]
    movsd xmm1, qword ptr [rdx + r11*8]

    ; xmm1 = rate[i] / periodDivisor  => rate[i] * invDiv
    mulsd xmm1, xmm4

    ; xmm1 = 1 + (rate/periodDivisor)
    addsd xmm1, qword ptr [OneConst]

    ; xmm0 = debt * (1 + rate/periodDivisor)
    mulsd xmm0, xmm1

    ; outDebt[i] = xmm0
    movsd qword ptr [r9 + r11*8], xmm0

    inc r10d
    jmp loop_start

done:
    xor eax, eax
    ret

fail:
    mov eax, 1
    ret
ProjectDebtAsm ENDP

.data
align 8
OneConst dq 3FF0000000000000h ; 1.0

END
