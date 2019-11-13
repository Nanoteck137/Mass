; ModuleID = 'NO NAME'
source_filename = "NO NAME"

%struct.FILE = type opaque

@str = private unnamed_addr constant [11 x i8] c"Value: %f\0A\00", align 1

declare i32 @printf(i8*, ...)

declare %struct.FILE* @fopen(i8*, i8*)

declare i64 @fwrite(i8*, i64, i64, %struct.FILE*)

declare i32 @fclose(%struct.FILE*)

declare i64 @strlen(i8*)

define i32 @main(i32 %argc, i8** %argv) {
  %argc1 = alloca i32
  store i32 %argc, i32* %argc1
  %argv2 = alloca i8**
  store i8** %argv, i8*** %argv2
  %x = alloca float
  store float 0x40091EB860000000, float* %x
  %y = alloca i32
  store i32 3, i32* %y
  %z = alloca float
  %1 = load float, float* %x
  %2 = load i32, i32* %y
  %3 = uitofp i32 %2 to float
  %4 = fadd float %1, %3
  store float %4, float* %z
  %5 = load float, float* %z
  %6 = fpext float %5 to double
  %7 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([11 x i8], [11 x i8]* @str, i32 0, i32 0), double %6)
  ret i32 0
}
