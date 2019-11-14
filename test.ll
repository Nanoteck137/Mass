; ModuleID = 'NO NAME'
source_filename = "NO NAME"

%struct.FILE = type opaque

@str = private unnamed_addr constant [17 x i8] c"Y is equal to 4\0A\00", align 1
@str.1 = private unnamed_addr constant [17 x i8] c"X is equal to 3\0A\00", align 1
@str.2 = private unnamed_addr constant [17 x i8] c"x is equal to 2\0A\00", align 1

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
  %x = alloca i32
  store i32 3, i32* %x
  %y = alloca i32
  store i32 4, i32* %y
  %0 = load i32, i32* %x
  %1 = icmp eq i32 %0, 3
  br i1 %1, label %then, label %endif

then3:                                            ; preds = %then
  %2 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([17 x i8], [17 x i8]* @str, i32 0, i32 0))
  br label %endif4

endif4:                                           ; preds = %then3, %then
  %3 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([17 x i8], [17 x i8]* @str.1, i32 0, i32 0))
  %4 = load i32, i32* %x
  %5 = sub i32 %4, 1
  store i32 %5, i32* %x
  br label %endif

then:                                             ; preds = %entry
  %6 = load i32, i32* %y
  %7 = icmp eq i32 %6, 4
  br i1 %7, label %then3, label %endif4

endif:                                            ; preds = %endif4, %entry
  %8 = load i32, i32* %x
  %9 = icmp eq i32 %8, 2
  br i1 %9, label %then5, label %endif6

then5:                                            ; preds = %endif
  %10 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([17 x i8], [17 x i8]* @str.2, i32 0, i32 0))
  br label %endif6

endif6:                                           ; preds = %then5, %endif
  ret i32 0
}
