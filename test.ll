; ModuleID = 'NO NAME'
source_filename = "NO NAME"

%struct.FILE = type opaque

@str = private unnamed_addr constant [9 x i8] c"Val: %u\0A\00", align 1

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
  %a = alloca i8
  store i8 -1, i8* %a
  %0 = load i8, i8* %a
  %1 = zext i8 %0 to i32
  %2 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([9 x i8], [9 x i8]* @str, i32 0, i32 0), i32 %1)
  ret i32 0
}
