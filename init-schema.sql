-- ═══════════════════════════════════════════════════════════════
-- SCHÉMA COMPLET LeoniRFID — Supabase Locale
-- ═══════════════════════════════════════════════════════════════

-- TABLE 1 : Profils utilisateurs
CREATE TABLE IF NOT EXISTS public.profiles (
    id UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
    full_name TEXT NOT NULL DEFAULT '',
    role TEXT NOT NULL DEFAULT 'Technician' CHECK (role IN ('Admin', 'Technician', 'Maintenance')),
    is_active BOOLEAN NOT NULL DEFAULT true,
    must_change_password BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- TABLE 2 : Départements
CREATE TABLE IF NOT EXISTS public.departments (
    id SERIAL PRIMARY KEY,
    code TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL DEFAULT '',
    description TEXT
);

-- TABLE 3 : Machines industrielles
CREATE TABLE IF NOT EXISTS public.machines (
    id SERIAL PRIMARY KEY,
    tag_id TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL DEFAULT '',
    department TEXT NOT NULL DEFAULT '',
    status TEXT NOT NULL DEFAULT 'Running' CHECK (status IN ('Running', 'Broken', 'InMaintenance', 'Paused', 'Removed')),
    installation_date TIMESTAMPTZ NOT NULL DEFAULT now(),
    exit_date TIMESTAMPTZ,
    notes TEXT,
    last_updated TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- TABLE 4 : Événements de scan RFID
CREATE TABLE IF NOT EXISTS public.scan_events (
    id SERIAL PRIMARY KEY,
    tag_id TEXT NOT NULL DEFAULT '',
    machine_id INTEGER REFERENCES public.machines(id),
    user_id UUID REFERENCES auth.users(id),
    event_type TEXT NOT NULL DEFAULT 'Scan',
    timestamp TIMESTAMPTZ NOT NULL DEFAULT now(),
    notes TEXT
);

-- TABLE 5 : Sessions de maintenance
CREATE TABLE IF NOT EXISTS public.maintenance_sessions (
    id SERIAL PRIMARY KEY,
    machine_id INTEGER NOT NULL REFERENCES public.machines(id),
    technician_id UUID REFERENCES auth.users(id),
    started_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    ended_at TIMESTAMPTZ,
    duration_minutes DOUBLE PRECISION,
    notes TEXT
);

-- TRIGGER : Créer automatiquement un profil quand un utilisateur s'inscrit
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS TRIGGER AS $$
BEGIN
    INSERT INTO public.profiles (id, full_name, role, must_change_password)
    VALUES (
        NEW.id,
        COALESCE(NEW.raw_user_meta_data->>'full_name', ''),
        COALESCE(NEW.raw_user_meta_data->>'role', 'Technician'),
        true
    );
    RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

DROP TRIGGER IF EXISTS on_auth_user_created ON auth.users;
CREATE TRIGGER on_auth_user_created
    AFTER INSERT ON auth.users
    FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();

-- ═══════════════════════════════════════════════════════════════
-- POLITIQUES RLS
-- ═══════════════════════════════════════════════════════════════

ALTER TABLE public.profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.machines ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.scan_events ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.maintenance_sessions ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.departments ENABLE ROW LEVEL SECURITY;

-- Profiles
CREATE POLICY "Authenticated users can view all profiles" ON public.profiles FOR SELECT USING (auth.role() = 'authenticated');
CREATE POLICY "Users can update own profile" ON public.profiles FOR UPDATE USING (auth.uid() = id);

-- Machines
CREATE POLICY "Authenticated users can view all machines" ON public.machines FOR SELECT USING (auth.role() = 'authenticated');
CREATE POLICY "Authenticated users can insert machines" ON public.machines FOR INSERT WITH CHECK (auth.role() = 'authenticated');
CREATE POLICY "Authenticated users can update machines" ON public.machines FOR UPDATE USING (auth.role() = 'authenticated');
CREATE POLICY "Authenticated users can delete machines" ON public.machines FOR DELETE USING (auth.role() = 'authenticated');

-- Scan Events
CREATE POLICY "Authenticated users can view scan events" ON public.scan_events FOR SELECT USING (auth.role() = 'authenticated');
CREATE POLICY "Authenticated users can insert scan events" ON public.scan_events FOR INSERT WITH CHECK (auth.role() = 'authenticated');

-- Maintenance Sessions
CREATE POLICY "Authenticated users can view maintenance" ON public.maintenance_sessions FOR SELECT USING (auth.role() = 'authenticated');
CREATE POLICY "Authenticated users can insert maintenance" ON public.maintenance_sessions FOR INSERT WITH CHECK (auth.role() = 'authenticated');
CREATE POLICY "Authenticated users can update maintenance" ON public.maintenance_sessions FOR UPDATE USING (auth.role() = 'authenticated');

-- Departments
CREATE POLICY "Authenticated users can view departments" ON public.departments FOR SELECT USING (auth.role() = 'authenticated');

-- ═══════════════════════════════════════════════════════════════
-- DONNÉES DE TEST
-- ═══════════════════════════════════════════════════════════════

-- Départements
INSERT INTO public.departments (code, name, description) VALUES
    ('LTN1', 'LEONI Tunisie 1', 'Site de production principal'),
    ('LTN2', 'LEONI Tunisie 2', 'Site de production secondaire'),
    ('LTN3', 'LEONI Tunisie 3', 'Site de production tertiaire')
ON CONFLICT (code) DO NOTHING;

-- Machines de test
INSERT INTO public.machines (tag_id, name, department, status) VALUES
    ('E200001', 'Machine Coupe A1', 'LTN1', 'Running'),
    ('E200002', 'Machine Sertissage B2', 'LTN1', 'Running'),
    ('E200003', 'Machine Assemblage C3', 'LTN2', 'Running'),
    ('E200004', 'Presse Hydraulique D4', 'LTN2', 'Broken'),
    ('E200005', 'Robot Soudure E5', 'LTN3', 'Running')
ON CONFLICT (tag_id) DO NOTHING;
