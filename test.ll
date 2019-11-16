; ModuleID = 'NO NAME'
source_filename = "NO NAME"

%struct.FILE = type opaque

@str = private unnamed_addr constant [7 x i8] c"B: %u\0A\00", align 1
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
  store float 0x40091EB860000000, float* %a
  %b = alloca i32
  %0 = load float, float* %a
  %1 = fadd float %0, 0x40091EB860000000
  %2 = fptoui float %1 to i32
  store i32 %2, i32* %b
  %c = alloca float
  %3 = load i32, i32* %b
  %4 = uitofp i32 %3 to float
  store float %4, float* %c
  %5 = load i32, i32* %b
  %6 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @str, i32 0, i32 0), i32 %5)
  %7 = load float, float* %c
  %8 = fpext float %7 to double
  %9 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @str.1, i32 0, i32 0), double %8)
  ret i32 0
}
