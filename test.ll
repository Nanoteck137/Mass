; ModuleID = 'NO NAME'
source_filename = "NO NAME"

%struct.FILE = type opaque

@str = private unnamed_addr constant [7 x i8] c"X: %f\0A\00", align 1
@str.1 = private unnamed_addr constant [7 x i8] c"X: %f\0A\00", align 1

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
  %x = alloca float
  store float 0x40091EB860000000, float* %x
  %0 = load float, float* %x
  %1 = fpext float %0 to double
  %2 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @str, i32 0, i32 0), double %1)
  %3 = load float, float* %x
  %4 = fadd float %3, 1.000000e+01
  store float %4, float* %x
  %5 = load float, float* %x
  %6 = fpext float %5 to double
  %7 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @str.1, i32 0, i32 0), double %6)
  ret i32 0
}
