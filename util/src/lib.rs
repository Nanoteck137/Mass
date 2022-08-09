//! Utility crate

#[derive(Clone, Debug)]
pub struct P<T: ?Sized> {
    ptr: Box<T>,
}

impl<T: ?Sized> P<T> {
    pub fn new(ptr: Box<T>) -> Self {
        Self { ptr }
    }
}

impl<T: ?Sized> std::ops::Deref for P<T> {
    type Target = T;

    fn deref(&self) -> &T {
        &self.ptr
    }
}

impl<T: ?Sized> std::ops::DerefMut for P<T> {
    fn deref_mut(&mut self) -> &mut T {
        &mut self.ptr
    }
}
