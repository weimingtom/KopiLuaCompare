
		public static int sizeCclosure(int n) {
			return GetUnmanagedSize(typeof(CClosure)) + GetUnmanagedSize(typeof(TValue)) * (n - 1);
		}

		public static int sizeLclosure(int n) {
			return GetUnmanagedSize(typeof(LClosure)) + GetUnmanagedSize(typeof(TValue)) * (n - 1);
		}
