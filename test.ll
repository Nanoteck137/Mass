; ModuleID = 'NO NAME'
source_filename = "NO NAME"

%struct.FILE = type opaque

@str = private unnamed_addr constant [7 x i8] c"C: %f\0A\00", align 1

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
  %4 = load float, float* %c
  %5 = fpext float %4 to double
  %6 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @str, i32 0, i32 0), double %5)
  ret i32 0
}
