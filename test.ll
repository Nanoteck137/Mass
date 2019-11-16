; ModuleID = 'NO NAME'
source_filename = "NO NAME"

%struct.FILE = type opaque

@str = private unnamed_addr constant [7 x i8] c"I: %d\0A\00", align 1
@str.1 = private unnamed_addr constant [7 x i8] c"C: %f\0A\00", align 1

declare i32 @printf(i8*, ...)

declare %struct.FILE* @fopen(i8*, i8*)

declare i64 @fwrite(i8*, i64, i64, %struct.FILE*)

declare i32 @fclose(%struct.FILE*)

declare i64 @strlen(i8*)

define i32 @main(i32 %argc, i8** %argv) {
entry:
  %argc1 = alloca i32
  store i32 %argc, i32* %argc1
  %argv2 = alloca i8**
  store i8** %argv, i8*** %argv2
  %a = alloca float
  store float 0xC0091EB860000000, float* %a
  %b = alloca i32
  store i32 3, i32* %b
  %c = alloca float
  %0 = load float, float* %a
  %1 = load i32, i32* %b
  %2 = sitofp i32 %1 to float
  %3 = fadd float %0, %2
  store float %3, float* %c
  %i = alloca i32
  store i32 0, i32* %i
  br label %for

then3:                                            ; preds = %then
  br label %endfor

endif:                                            ; preds = %then
  %4 = load i32, i32* %i
  %5 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @str, i32 0, i32 0), i32 %4)
  br label %next

for:                                              ; preds = %next, %entry
  %6 = load i32, i32* %i
  %7 = icmp slt i32 %6, 10
  br i1 %7, label %then, label %endfor

then:                                             ; preds = %for
  %8 = load i32, i32* %i
  %9 = icmp eq i32 %8, 2
  br i1 %9, label %then3, label %endif

next:                                             ; preds = %endif
  %10 = load i32, i32* %i
  %11 = add i32 %10, 1
  store i32 %11, i32* %i
  br label %for

endfor:                                           ; preds = %then3, %for
  %12 = load float, float* %c
  %13 = fpext float %12 to double
  %14 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @str.1, i32 0, i32 0), double %13)
  ret i32 0
}
