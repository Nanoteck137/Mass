	.text
	.file	"test.ma"
	.globl	test                    # -- Begin function test
	.p2align	4, 0x90
	.type	test,@function
test:                                   # @test
	.cfi_startproc
# %bb.0:                                # %entry
	pushq	%rax
	.cfi_def_cfa_offset 16
	movl	$.L__unnamed_1, %edi
	xorl	%eax, %eax
	callq	printf
	popq	%rax
	retq
.Lfunc_end0:
	.size	test, .Lfunc_end0-test
	.cfi_endproc
                                        # -- End function
	.globl	add                     # -- Begin function add
	.p2align	4, 0x90
	.type	add,@function
add:                                    # @add
	.cfi_startproc
# %bb.0:                                # %entry
	subq	$24, %rsp
	.cfi_def_cfa_offset 32
	movl	%edi, 20(%rsp)
	movl	%esi, 16(%rsp)
	addl	%esi, %edi
	movl	%edi, 12(%rsp)
	callq	test
	movl	20(%rsp), %esi
	movl	16(%rsp), %edx
	movl	12(%rsp), %ecx
	movl	$.L__unnamed_2, %edi
	xorl	%eax, %eax
	callq	printf
	movl	12(%rsp), %eax
	addq	$24, %rsp
	retq
.Lfunc_end1:
	.size	add, .Lfunc_end1-add
	.cfi_endproc
                                        # -- End function
	.type	.L__unnamed_1,@object   # @0
	.section	.rodata.str1.1,"aMS",@progbits,1
.L__unnamed_1:
	.asciz	"Hello World\n"
	.size	.L__unnamed_1, 13

	.type	.L__unnamed_2,@object   # @1
.L__unnamed_2:
	.asciz	"%d + %d = %d\n"
	.size	.L__unnamed_2, 14


	.section	".note.GNU-stack","",@progbits
